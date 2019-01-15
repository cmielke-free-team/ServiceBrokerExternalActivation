$ErrorActionPreference = 'Stop';

$packageName= $env:ChocolateyPackageName
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$serviceName = "SSBExternalActivator.DATA_Broker.BE"
$serviceDisplayName = "Service Broker External Activator (DATA_Broker.BE)"
$sourceFolder = "C:\Program Files\Service Broker\External Activator\"
$destinationFolder = "C:\Program Files\Service Broker\$serviceName"
$storedProcedure = "dbo.Receive_Messages_BE_NotificationQueue"
$logFolder = "C:\Log\$serviceName"

if (!(Test-Path $logFolder))
{
    New-Item -Path $logFolder -ItemType directory
}

if(!(Test-Path $sourceFolder))
{
    throw "The source folder does not exist: $sourceFolder. Make sure the default instance of SSBEA was successfully installed."
}

$svc = 
    Get-Service | 
    where { $_.Name -ieq $serviceName } | 
    where { $_.Status -ieq "Running" } |
    Stop-Service -PassThru -Force    

if(!(Test-Path $destinationFolder))
{
    #create folders for new ssbea installations
    new-item $destinationFolder -Force -ItemType Directory -Verbose
}

$configFilePath = join-path $destinationFolder "Config\EAService.config"
if(Test-Path $configFilePath)
{
    $tempFolder = new-item (join-path $env:TEMP "$(new-guid)") -ItemType Directory
    $tempConfigFile = copy-item $configFilePath $tempFolder -Force -PassThru
    $xml = [xml](Get-Content $tempConfigFile)
    $nsman = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $nsman.AddNamespace("ea", $xml.DocumentElement.NamespaceURI)
    if (!$xml.Activator.SelectSingleNode("ea:StoredProcedure", $nsman))
    {
        $element = $xml.CreateElement("StoredProcedure", $xml.DocumentElement.NamespaceURI)
        $element.InnerText = $storedProcedure
        $xml.DocumentElement.AppendChild($element)
    }
	else
	{
		$spElement = $xml.Activator.SelectSingleNode("ea:StoredProcedure", $nsman);
		$spElement.InnerText = $storedProcedure
	}
    $xml.Save($tempConfigFile)
}

#copy ssbea binaries
copy-item -Path (join-path $sourceFolder "*") -Destination $destinationFolder -Recurse -Force -Verbose

#copy custom ssbea binaries
$exclude = @('*.ps1')
copy-item -Path (join-path $toolsDir "*") -Destination (join-path $destinationFolder -ChildPath "bin") -Exclude $exclude -Recurse -Force -Verbose

#configure ssbea
    $eaconfigtext = @"
<?xml version="1.0" encoding="utf-8"?>
<Activator xmlns="http://schemas.microsoft.com/sqlserver/2008/10/servicebroker/externalactivator" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="http://schemas.microsoft.com/sqlserver/2008/10/servicebroker/externalactivator EAServiceConfig.xsd">
  <StoredProcedure>$storedProcedure</StoredProcedure>
  <NotificationServiceList>
  </NotificationServiceList>
  <ApplicationServiceList>
  </ApplicationServiceList>
  <LogSettings>
    <LogFilter>
    </LogFilter>
  </LogSettings>
</Activator>
"@    
if(!$tempConfigFile)
{
    Set-Content (join-path $destinationFolder "Config\EAService.config") -Value $eaconfigtext -Force -Verbose
}
else
{
    copy-item $tempConfigFile $configFilePath -Force
}

#create event log sources if needed
if(!(Get-EventLog -LogName "Application" -Source $serviceName -ErrorAction SilentlyContinue))
{    
    New-EventLog -LogName "Application" -Source $serviceName -ErrorAction SilentlyContinue
}

#If the service exists but isn't using the right executable path, remove it 
$exePath = Get-WmiObject win32_service | ?{$_.Name -ieq $serviceName } | select -ExpandProperty PathName
$desiredExePath = ($(join-path $destinationFolder "bin\EmdatSSBEAService.exe")).ToString()
$desiredExePathWithQuotes = "`"$desiredExePath`""
$desiredExePathAndArgs = $desiredExePathWithQuotes + " /service:" + $serviceName
if ($exePath -and $exePath -ne $desiredExePathAndArgs)
{
    Write-Host "The service existed for the wrong executable and will be removed."
    $service = Get-WmiObject win32_service | ?{$_.Name -ieq $serviceName }
    $service.delete()
}

#configure app.config
$appConfigFile = join-path $destinationFolder "bin\EmdatSSBEAService.exe.config"
[xml]$appConfig = Get-Content $appConfigFile
$eventLogNode = $appConfig.configuration.'system.diagnostics'.sharedListeners.add | where {$_.name -ieq "eventlog"}
$eventLogNode.initializeData = $serviceName

$fileLogNode = $appConfig.configuration.'system.diagnostics'.sharedListeners.add | where {$_.name -ieq "file"}
$fileLogNode.initializeData = "$logFolder\{MachineName}-{ApplicationName}-{LocalDateTime:yyyyMMddHH}-{ProcessId}.log"

$appConfig.Save($appConfigFile)


#Create the service if it does not exist
if (!(Get-Service $serviceName -ErrorAction SilentlyContinue))
{
    Write-Host "The service did not exist and will be created."
    New-Service -Name $serviceName -BinaryPathName "`"$(join-path $destinationFolder "bin\EmdatSSBEAService.exe")`" /service:$serviceName" -DisplayName $serviceDisplayName -StartupType Automatic -Verbose
}

#grant full control on the service folder to the service sid
& sc.exe sidtype "$serviceName" unrestricted
$accountName = "NT SERVICE\$serviceName"
$fileSystemRights = [Security.AccessControl.FileSystemRights]::FullControl
$inheritanceFlags = ([Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [Security.AccessControl.InheritanceFlags]::ObjectInherit)
$propagationFlags = [Security.AccessControl.PropagationFlags]::None
$accessControlType = [Security.AccessControl.AccessControlType]::Allow
$fileSystemAccessRule = new-object System.Security.AccessControl.FileSystemAccessRule ($accountName,$fileSystemRights,$inheritanceFlags,$propagationFlags,$accessControlType)
$acl = get-acl $destinationFolder
$acl.SetAccessRule($fileSystemAccessRule)
Set-Acl -Path $destinationFolder -AclObject $acl

#start the service (if it was running before)
if($svc) 
{
    Get-Service $serviceName | start-service -Verbose -passthru
}