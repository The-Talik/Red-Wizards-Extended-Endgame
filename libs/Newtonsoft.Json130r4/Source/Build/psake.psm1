# psake
# Copyright (c) 2012 James Kovacs
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.

#Requires -Version 2.0

#-- Public Module Functions --#

# .ExternalHelp  psake.psm1-help.xml
function Invoke-Task
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)] [string]$taskName
    )

    Assert $taskName ($msgs.error_invalid_task_name)

    $taskKey = $taskName.ToLower()

    if ($currentContext.aliases.Contains($taskKey)) {
        $taskName = $currentContext.aliases.$taskKey.Name
        $taskKey = $taskName.ToLower()
    }

    $currentContext = $psake.context.Peek()

    Assert ($currentContext.tasks.Contains($taskKey)) ($msgs.error_task_name_does_not_exist -f $taskName)

    if ($currentContext.executedTasks.Contains($taskKey))  { return }

    Assert (!$currentContext.callStack.Contains($taskKey)) ($msgs.error_circular_reference -f $taskName)

    $currentContext.callStack.Push($taskKey)

    $task = $currentContext.tasks.$taskKey

    $precondition_is_valid = & $task.Precondition

    if (!$precondition_is_valid) {
        WriteColoredOutput ($msgs.precondition_was_false -f $taskName) -foregroundcolor Cyan
    } else {
        if ($taskKey -ne 'default') {

            if ($task.PreAction -or $task.PostAction) {
                Assert ($task.Action -ne $null) ($msgs.error_missing_action_parameter -f $taskName)
            }

            if ($task.Action) {
                try {
                    foreach($childTask in $task.DependsOn) {
                        Invoke-Task $childTask
                    }

                    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
                    $currentContext.currentTaskName = $taskName

                    & $currentContext.taskSetupScriptBlock

                    if ($task.PreAction) {
                        & $task.PreAction
                    }

                    if ($currentContext.config.taskNameFormat -is [ScriptBlock]) {
                        & $currentContext.config.taskNameFormat $taskName
                    } else {
                        WriteColoredOutput ($currentContext.config.taskNameFormat -f $taskName) -foregroundcolor Cyan
                    }

                    foreach ($variable in $task.requiredVariables) {
                        Assert ((test-path "variable:$variable") -and ((get-variable $variable).Value -ne $null)) ($msgs.required_variable_not_set -f $variable, $taskName)
                    }

                    & $task.Action

                    if ($task.PostAction) {
                        & $task.PostAction
                    }

                    & $currentContext.taskTearDownScriptBlock
                    $task.Duration = $stopwatch.Elapsed
                } catch {
                    if ($task.ContinueOnError) {
                        "-"*70
                        WriteColoredOutput ($msgs.continue_on_error -f $taskName,$_) -foregroundcolor Yellow
                        "-"*70
                        $task.Duration = $stopwatch.Elapsed
                    }  else {
                        throw $_
                    }
                }
            } else {
                # no action was specified but we still execute all the dependencies
                foreach($childTask in $task.DependsOn) {
                    Invoke-Task $childTask
                }
            }
        } else {
            foreach($childTask in $task.DependsOn) {
                Invoke-Task $childTask
            }
        }

        Assert (& $task.Postcondition) ($msgs.postcondition_failed -f $taskName)
    }

    $poppedTaskKey = $currentContext.callStack.Pop()
    Assert ($poppedTaskKey -eq $taskKey) ($msgs.error_corrupt_callstack -f $taskKey,$poppedTaskKey)

    $currentContext.executedTasks.Push($taskKey)
}

# .ExternalHelp  psake.psm1-help.xml
function Exec
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ($msgs.error_bad_command -f $cmd),
        [Parameter(Position=2,Mandatory=0)][int]$maxRetries = 0,
        [Parameter(Position=3,Mandatory=0)][string]$retryTriggerErrorPattern = $null
    )

    $tryCount = 1

    do {
        try {
            $global:lastexitcode = 0
            & $cmd
            if ($lastexitcode -ne 0) {
                throw ("Exec: " + $errorMessage)
            }
            break
        }
        catch [Exception]
        {
            if ($tryCount -gt $maxRetries) {
                throw $_
            }

            if ($retryTriggerErrorPattern -ne $null) {
                $isMatch = [regex]::IsMatch($_.Exception.Message, $retryTriggerErrorPattern)

                if ($isMatch -eq $false) {
                    throw $_
                }
            }

            Write-Host "Try $tryCount failed, retrying again in 1 second..."

            $tryCount++

            [System.Threading.Thread]::Sleep([System.TimeSpan]::FromSeconds(1))
        }
    }
    while ($true)
}

# .ExternalHelp  psake.psm1-help.xml
function Assert
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)]$conditionToCheck,
        [Parameter(Position=1,Mandatory=1)]$failureMessage
    )
    if (!$conditionToCheck) {
        throw ("Assert: " + $failureMessage)
    }
}

# .ExternalHelp  psake.psm1-help.xml
function Task
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][string]$name = $null,
        [Parameter(Position=1,Mandatory=0)][scriptblock]$action = $null,
        [Parameter(Position=2,Mandatory=0)][scriptblock]$preaction = $null,
        [Parameter(Position=3,Mandatory=0)][scriptblock]$postaction = $null,
        [Parameter(Position=4,Mandatory=0)][scriptblock]$precondition = {$true},
        [Parameter(Position=5,Mandatory=0)][scriptblock]$postcondition = {$true},
        [Parameter(Position=6,Mandatory=0)][switch]$continueOnError = $false,
        [Parameter(Position=7,Mandatory=0)][string[]]$depends = @(),
        [Parameter(Position=8,Mandatory=0)][string[]]$requiredVariables = @(),
        [Parameter(Position=9,Mandatory=0)][string]$description = $null,
        [Parameter(Position=10,Mandatory=0)][string]$alias = $null,
        [Parameter(Position=11,Mandatory=0)][string]$maxRetries = 0,
        [Parameter(Position=12,Mandatory=0)][string]$retryTriggerErrorPattern = $null
    )
    if ($name -eq 'default') {
        Assert (!$action) ($msgs.error_default_task_cannot_have_action)
    }

    $newTask = @{
        Name = $name
        DependsOn = $depends
        PreAction = $preaction
        Action = $action
        PostAction = $postaction
        Precondition = $precondition
        Postcondition = $postcondition
        ContinueOnError = $continueOnError
        Description = $description
        Duration = [System.TimeSpan]::Zero
        RequiredVariables = $requiredVariables
        Alias = $alias
        MaxRetries = $maxRetries
        RetryTriggerErrorPattern = $retryTriggerErrorPattern
    }

    $taskKey = $name.ToLower()

    $currentContext = $psake.context.Peek()

    Assert (!$currentContext.tasks.ContainsKey($taskKey)) ($msgs.error_duplicate_task_name -f $name)

    $currentContext.tasks.$taskKey = $newTask

    if($alias)
    {
        $aliasKey = $alias.ToLower()

        Assert (!$currentContext.aliases.ContainsKey($aliasKey)) ($msgs.error_duplicate_alias_name -f $alias)

        $currentContext.aliases.$aliasKey = $newTask
    }
}

# .ExternalHelp  psake.psm1-help.xml
function Properties {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$properties
    )
    $psake.context.Peek().properties += $properties
}

# .ExternalHelp  psake.psm1-help.xml
function Include {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][string]$fileNamePathToInclude
    )
    Assert (test-path $fileNamePathToInclude -pathType Leaf) ($msgs.error_invalid_include_path -f $fileNamePathToInclude)
    $psake.context.Peek().includes.Enqueue((Resolve-Path $fileNamePathToInclude));
}

# .ExternalHelp  psake.psm1-help.xml
function FormatTaskName {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)]$format
    )
    $psake.context.Peek().config.taskNameFormat = $format
}

# .ExternalHelp  psake.psm1-help.xml
function TaskSetup {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$setup
    )
    $psake.context.Peek().taskSetupScriptBlock = $setup
}

# .ExternalHelp  psake.psm1-help.xml
function TaskTearDown {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$teardown
    )
    $psake.context.Peek().taskTearDownScriptBlock = $teardown
}

# .ExternalHelp  psake.psm1-help.xml
function Framework {
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][string]$framework
    )
    $psake.context.Peek().config.framework = $framework
    ConfigureBuildEnvironment
}

# .ExternalHelp  psake.psm1-help.xml
function Invoke-psake {
    [CmdletBinding()]
    param(
        [Parameter(Position = 0, Mandatory = 0)][string] $buildFile,
        [Parameter(Position = 1, Mandatory = 0)][string[]] $taskList = @(),
        [Parameter(Position = 2, Mandatory = 0)][string] $framework,
        [Parameter(Position = 3, Mandatory = 0)][switch] $docs = $false,
        [Parameter(Position = 4, Mandatory = 0)][hashtable] $parameters = @{},
        [Parameter(Position = 5, Mandatory = 0)][hashtable] $properties = @{},
        [Parameter(Position = 6, Mandatory = 0)][alias("init")][scriptblock] $initialization = {},
        [Parameter(Position = 7, Mandatory = 0)][switch] $nologo = $false,
        [Parameter(Position = 8, Mandatory = 0)][switch] $detailedDocs = $false
    )
    try {
        if (-not $nologo) {
            "psake version {0}`nCopyright (c) 2010-2015 James Kovacs, Damian Hickey & Contributors`n" -f $psake.version
        }

        if (!$buildFile) {
          $buildFile = $psake.config_default.buildFileName
        }
        elseif (!(test-path $buildFile -pathType Leaf) -and (test-path $psake.config_default.buildFileName -pathType Leaf)) {
            # If the $config.buildFileName file exists and the given "buildfile" isn 't found assume that the given
            # $buildFile is actually the target Tasks to execute in the $config.buildFileName script.
            $taskList = $buildFile.Split(', ')
            $buildFile = $psake.config_default.buildFileName
        }

        # Execute the build file to set up the tasks and defaults
        Assert (test-path $buildFile -pathType Leaf) ($msgs.error_build_file_not_found -f $buildFile)

        $psake.build_script_file = get-item $buildFile
        $psake.build_script_dir = $psake.build_script_file.DirectoryName
        $psake.build_success = $false

        $psake.context.push(@{
            "taskSetupScriptBlock" = {};
            "taskTearDownScriptBlock" = {};
            "executedTasks" = new-object System.Collections.Stack;
            "callStack" = new-object System.Collections.Stack;
            "originalEnvPath" = $env:path;
            "originalDirectory" = get-location;
            "originalErrorActionPreference" = $global:ErrorActionPreference;
            "tasks" = @{};
            "aliases" = @{};
            "properties" = @();
            "includes" = new-object System.Collections.Queue;
            "config" = CreateConfigurationForNewContext $buildFile $framework
        })

        LoadConfiguration $psake.build_script_dir

        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

        set-location $psake.build_script_dir

        LoadModules

        $frameworkOldValue = $framework
        . $psake.build_script_file.FullName

        $currentContext = $psake.context.Peek()

        if ($framework -ne $frameworkOldValue) {
            writecoloredoutput $msgs.warning_deprecated_framework_variable -foregroundcolor Yellow
            $currentContext.config.framework = $framework
        }

        ConfigureBuildEnvironment

        while ($currentContext.includes.Count -gt 0) {
            $includeFilename = $currentContext.includes.Dequeue()
            . $includeFilename
        }

        if ($docs -or $detailedDocs) {
            WriteDocumentation($detailedDocs)
            CleanupEnvironment
            return
        }

        foreach ($key in $parameters.keys) {
            if (test-path "variable:\$key") {
                set-item -path "variable:\$key" -value $parameters.$key -WhatIf:$false -Confirm:$false | out-null
            } else {
                new-item -path "variable:\$key" -value $parameters.$key -WhatIf:$false -Confirm:$false | out-null
            }
        }

        # The initial dot (.) indicates that variables initialized/modified in the propertyBlock are available in the parent scope.
        foreach ($propertyBlock in $currentContext.properties) {
            . $propertyBlock
        }

        foreach ($key in $properties.keys) {
            if (test-path "variable:\$key") {
                set-item -path "variable:\$key" -value $properties.$key -WhatIf:$false -Confirm:$false | out-null
            }
        }

        # Simple dot sourcing will not work. We have to force the script block into our
        # module's scope in order to initialize variables properly.
        . $MyInvocation.MyCommand.Module $initialization

        # Execute the list of tasks or the default task
        if ($taskList) {
            foreach ($task in $taskList) {
                invoke-task $task
            }
        } elseif ($currentContext.tasks.default) {
            invoke-task default
        } else {
            throw $msgs.error_no_default_task
        }

        WriteColoredOutput ("`n" + $msgs.build_success + "`n") -foregroundcolor Green

        WriteTaskTimeSummary $stopwatch.Elapsed

        $psake.build_success = $true
    } catch {
        $currentConfig = GetCurrentConfigurationOrDefault
        if ($currentConfig.verboseError) {
            $error_message = "{0}: An Error Occurred. See Error Details Below: `n" -f (Get-Date)
            $error_message += ("-" * 70) + "`n"
            $error_message += "Error: {0}`n" -f (ResolveError $_ -Short)
            $error_message += ("-" * 70) + "`n"
            $error_message += ResolveError $_
            $error_message += ("-" * 70) + "`n"
            $error_message += "Script Variables" + "`n"
            $error_message += ("-" * 70) + "`n"
            $error_message += get-variable -scope script | format-table | out-string
        } else {
            # ($_ | Out-String) gets error messages with source information included.
            $error_message = "Error: {0}: `n{1}" -f (Get-Date), (ResolveError $_ -Short)
        }

        $psake.build_success = $false

        # if we are running in a nested scope (i.e. running a psake script from a psake script) then we need to re-throw the exception
        # so that the parent script will fail otherwise the parent script will report a successful build
        $inNestedScope = ($psake.context.count -gt 1)
        if ( $inNestedScope ) {
            throw $_
        } else {
            if (!$psake.run_by_psake_build_tester) {
                WriteColoredOutput $error_message -foregroundcolor Red
            }
        }
    } finally {
        CleanupEnvironment
    }
}

#-- Private Module Functions --#
function WriteColoredOutput {
    param(
        [string] $message,
        [System.ConsoleColor] $foregroundcolor
    )

    $currentConfig = GetCurrentConfigurationOrDefault
    if ($currentConfig.coloredOutput -eq $true) {
        if (($Host.UI -ne $null) -and ($Host.UI.RawUI -ne $null) -and ($Host.UI.RawUI.ForegroundColor -ne $null)) {
            $previousColor = $Host.UI.RawUI.ForegroundColor
            $Host.UI.RawUI.ForegroundColor = $foregroundcolor
        }
    }

    $message

    if ($previousColor -ne $null) {
        $Host.UI.RawUI.ForegroundColor = $previousColor
    }
}

function LoadModules {
    $currentConfig = $psake.context.peek().config
    if ($currentConfig.modules) {

        $scope = $currentConfig.moduleScope

        $global = [string]::Equals($scope, "global", [StringComparison]::CurrentCultureIgnoreCase)

        $currentConfig.modules | foreach {
            resolve-path $_ | foreach {
                "Loading module: $_"
                $module = import-module $_ -passthru -DisableNameChecking -global:$global
                if (!$module) {
                    throw ($msgs.error_loading_module -f $_.Name)
                }
            }
        }
        ""
    }
}

function LoadConfiguration {
    param(
        [string] $configdir = $PSScriptRoot
    )

    $psakeConfigFilePath = (join-path $configdir "psake-config.ps1")

    if (test-path $psakeConfigFilePath -pathType Leaf) {
        try {
            $config = GetCurrentConfigurationOrDefault
            . $psakeConfigFilePath
        } catch {
            throw "Error Loading Configuration from psake-config.ps1: " + $_
        }
    }
}

function GetCurrentConfigurationOrDefault() {
    if ($psake.context.count -gt 0) {
        return $psake.context.peek().config
    } else {
        return $psake.config_default
    }
}

function CreateConfigurationForNewContext {
    param(
        [string] $buildFile,
        [string] $framework
    )

    $previousConfig = GetCurrentConfigurationOrDefault

    $config = new-object psobject -property @{
        buildFileName = $previousConfig.buildFileName;
        framework = $previousConfig.framework;
        taskNameFormat = $previousConfig.taskNameFormat;
        verboseError = $previousConfig.verboseError;
        coloredOutput = $previousConfig.coloredOutput;
        modules = $previousConfig.modules;
        moduleScope =  $previousConfig.moduleScope;
    }

    if ($framework) {
        $config.framework = $framework;
    }

    if ($buildFile) {
        $config.buildFileName = $buildFile;
    }

    return $config
}

function ConfigureBuildEnvironment {
    $framework = $psake.context.peek().config.framework
    if ($framework -cmatch '^((?:\d+\.\d+)(?:\.\d+){0,1})(x86|x64){0,1}$') {
        $versionPart = $matches[1]
        $bitnessPart = $matches[2]
    } else {
        throw ($msgs.error_invalid_framework -f $framework)
    }
    $versions = $null
    $buildToolsVersions = $null
    switch ($versionPart) {
        '1.0' {
            $versions = @('v1.0.3705')
        }
        '1.1' {
            $versions = @('v1.1.4322')
        }
        '2.0' {
            $versions = @('v2.0.50727')
        }
        '3.0' {
            $versions = @('v2.0.50727')
        }
        '3.5' {
            $versions = @('v3.5', 'v2.0.50727')
        }
        '4.0' {
            $versions = @('v4.0.30319')
        }
        {($_ -eq '4.5.1') -or ($_ -eq '4.5.2')} {
            $versions = @('v4.0.30319')
            $buildToolsVersions = @('14.0', '12.0')
        }
        '4.6' {
            $versions = @('v4.0.30319')
            $buildToolsVersions = @('14.0')
        }

        default {
            throw ($msgs.error_unknown_framework -f $versionPart, $framework)
        }
    }

    $bitness = 'Framework'
    if ($versionPart -ne '1.0' -and $versionPart -ne '1.1') {
        switch ($bitnessPart) {
            'x86' {
                $bitness = 'Framework'
                $buildToolsKey = 'MSBuildToolsPath32'
            }
            'x64' {
                $bitness = 'Framework64'
                $buildToolsKey = 'MSBuildToolsPath'
            }
            { [string]::IsNullOrEmpty($_) } {
                $ptrSize = [System.IntPtr]::Size
                switch ($ptrSize) {
                    4 {
                        $bitness = 'Framework'
                        $buildToolsKey = 'MSBuildToolsPath32'
                    }
                    8 {
                        $bitness = 'Framework64'
                        $buildToolsKey = 'MSBuildToolsPath'
                    }
                    default {
                        throw ($msgs.error_unknown_pointersize -f $ptrSize)
                    }
                }
            }
            default {
                throw ($msgs.error_unknown_bitnesspart -f $bitnessPart, $framework)
            }
        }
    }
    $frameworkDirs = @()
    if ($buildToolsVersions -ne $null) {
        foreach($ver in $buildToolsVersions) {
            if (Test-Path "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$ver") {
                $frameworkDirs += (Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\MSBuild\ToolsVersions\$ver" -Name $buildToolsKey).$buildToolsKey
            }
        }
    }
    $frameworkDirs = $frameworkDirs + @($versions | foreach { "$env:windir\Microsoft.NET\$bitness\$_\" })

    for ($i = 0; $i -lt $frameworkDirs.Count; $i++) {
        $dir = $frameworkDirs[$i]
        if ($dir -Match "\$\(Registry:HKEY_LOCAL_MACHINE(.*?)@(.*)\)") {
            $key = "HKLM:" + $matches[1]
            $name = $matches[2]
            $dir = (Get-ItemProperty -Path $key -Name $name).$name
            $frameworkDirs[$i] = $dir
        }
    }

    $frameworkDirs | foreach { Assert (test-path $_ -pathType Container) ($msgs.error_no_framework_install_dir_found -f $_)}

    $env:path = ($frameworkDirs -join ";") + ";$env:path"
    # if any error occurs in a PS function then "stop" processing immediately
    # this does not effect any external programs that return a non-zero exit code
    $global:ErrorActionPreference = "Stop"
}

function CleanupEnvironment {
    if ($psake.context.Count -gt 0) {
        $currentContext = $psake.context.Peek()
        $env:path = $currentContext.originalEnvPath
        Set-Location $currentContext.originalDirectory
        $global:ErrorActionPreference = $currentContext.originalErrorActionPreference
        [void] $psake.context.Pop()
    }
}

function SelectObjectWithDefault
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        [PSObject]
        $InputObject,
        [string]
        $Name,
        $Value
    )

    process {
        if ($_ -eq $null) { $Value }
        elseif ($_ | Get-Member -Name $Name) {
          $_.$Name
        }
        elseif (($_ -is [Hashtable]) -and ($_.Keys -contains $Name)) {
          $_.$Name
        }
        else { $Value }
    }
}

# borrowed from Jeffrey Snover http://blogs.msdn.com/powershell/archive/2006/12/07/resolve-error.aspx
# modified to better handle SQL errors
function ResolveError
{
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline=$true)]
        $ErrorRecord=$Error[0],
        [Switch]
        $Short
    )

    process {
        if ($_ -eq $null) { $_ = $ErrorRecord }
        $ex = $_.Exception

        if (-not $Short) {
            $error_message = "`nErrorRecord:{0}ErrorRecord.InvocationInfo:{1}Exception:`n{2}"
            $formatted_errorRecord = $_ | format-list * -force | out-string
            $formatted_invocationInfo = $_.InvocationInfo | format-list * -force | out-string
            $formatted_exception = ''

            $i = 0
            while ($ex -ne $null) {
                $i++
                $formatted_exception += ("$i" * 70) + "`n" +
                    ($ex | format-list * -force | out-string) + "`n"
                $ex = $ex | SelectObjectWithDefault -Name 'InnerException' -Value $null
            }

            return $error_message -f $formatted_errorRecord, $formatted_invocationInfo, $formatted_exception
        }

        $lastException = @()
        while ($ex -ne $null) {
            $lastMessage = $ex | SelectObjectWithDefault -Name 'Message' -Value ''
            $lastException += ($lastMessage -replace "`n", '')
            if ($ex -is [Data.SqlClient.SqlException]) {
                $lastException += "(Line [$($ex.LineNumber)] " +
                    "Procedure [$($ex.Procedure)] Class [$($ex.Class)] " +
                    " Number [$($ex.Number)] State [$($ex.State)] )"
            }
            $ex = $ex | SelectObjectWithDefault -Name 'InnerException' -Value $null
        }
        $shortException = $lastException -join ' --> '

        $header = $null
        $current = $_
        $header = (($_.InvocationInfo |
            SelectObjectWithDefault -Name 'PositionMessage' -Value '') -replace "`n", ' '),
            ($_ | SelectObjectWithDefault -Name 'Message' -Value ''),
            ($_ | SelectObjectWithDefault -Name 'Exception' -Value '') |
                ? { -not [String]::IsNullOrEmpty($_) } |
                Select -First 1

        $delimiter = ''
        if ((-not [String]::IsNullOrEmpty($header)) -and
            (-not [String]::IsNullOrEmpty($shortException)))
            { $delimiter = ' [<<==>>] ' }

        return "$($header)$($delimiter)Exception: $($shortException)"
    }
}

function WriteDocumentation($showDetailed) {
    $currentContext = $psake.context.Peek()

    if ($currentContext.tasks.default) {
        $defaultTaskDependencies = $currentContext.tasks.default.DependsOn
    } else {
        $defaultTaskDependencies = @()
    }

    $docs = $currentContext.tasks.Keys | foreach-object {
        if ($_ -eq "default") {
            return
        }

        $task = $currentContext.tasks.$_
        new-object PSObject -property @{
            Name = $task.Name;
            Alias = $task.Alias;
            Description = $task.Description;
            "Depends On" = $task.DependsOn -join ", "
            Default = if ($defaultTaskDependencies -contains $task.Name) { $true }
        }
    }
    if ($showDetailed) {
        $docs | sort 'Name' | format-list -property Name,Alias,Description,"Depends On",Default
    } else {
        $docs | sort 'Name' | format-table -autoSize -wrap -property Name,Alias,"Depends On",Default,Description
    }

}

function WriteTaskTimeSummary($invokePsakeDuration) {
    if ($psake.context.count -gt 0) {
        "-" * 70
        "Build Time Report"
        "-" * 70
        $list = @()
        $currentContext = $psake.context.Peek()
        while ($currentContext.executedTasks.Count -gt 0) {
            $taskKey = $currentContext.executedTasks.Pop()
            $task = $currentContext.tasks.$taskKey
            if ($taskKey -eq "default") {
                continue
            }
            $list += new-object PSObject -property @{
                Name = $task.Name;
                Duration = $task.Duration
            }
        }
        [Array]::Reverse($list)
        $list += new-object PSObject -property @{
            Name = "Total:";
            Duration = $invokePsakeDuration
        }
        # using "out-string | where-object" to filter out the blank line that format-table prepends
        $list | format-table -autoSize -property Name,Duration | out-string -stream | where-object { $_ }
    }
}

DATA msgs {
convertfrom-stringdata @'
    error_invalid_task_name = Task name should not be null or empty string.
    error_task_name_does_not_exist = Task {0} does not exist.
    error_circular_reference = Circular reference found for task {0}.
    error_missing_action_parameter = Action parameter must be specified when using PreAction or PostAction parameters for task {0}.
    error_corrupt_callstack = Call stack was corrupt. Expected {0}, but got {1}.
    error_invalid_framework = Invalid .NET Framework version, {0} specified.
    error_unknown_framework = Unknown .NET Framework version, {0} specified in {1}.
    error_unknown_pointersize = Unknown pointer size ({0}) returned from System.IntPtr.
    error_unknown_bitnesspart = Unknown .NET Framework bitness, {0}, specified in {1}.
    error_no_framework_install_dir_found = No .NET Framework installation directory found at {0}.
    error_bad_command = Error executing command {0}.
    error_default_task_cannot_have_action = 'default' task cannot specify an action.
    error_duplicate_task_name = Task {0} has already been defined.
    error_duplicate_alias_name = Alias {0} has already been defined.
    error_invalid_include_path = Unable to include {0}. File not found.
    error_build_file_not_found = Could not find the build file {0}.
    error_no_default_task = 'default' task required.
    error_loading_module = Error loading module {0}.
    warning_deprecated_framework_variable = Warning: Using global variable $framework to set .NET framework version used is deprecated. Instead use Framework function or configuration file psake-config.ps1.
    required_variable_not_set = Variable {0} must be set to run task {1}.
    postcondition_failed = Postcondition failed for task {0}.
    precondition_was_false = Precondition was false, not executing task {0}.
    continue_on_error = Error in task {0}. {1}
    build_success = Build Succeeded!
'@
}

import-localizeddata -bindingvariable msgs -erroraction silentlycontinue

$script:psake = @{}
$psake.version = "4.4.2" # contains the current version of psake
$psake.context = new-object system.collections.stack # holds onto the current state of all variables
$psake.run_by_psake_build_tester = $false # indicates that build is being run by psake-BuildTester
$psake.config_default = new-object psobject -property @{
    buildFileName = "default.ps1";
    framework = "4.0";
    taskNameFormat = "Executing {0}";
    verboseError = $false;
    coloredOutput = $true;
    modules = $null;
    moduleScope = "";
} # contains default configuration, can be overriden in psake-config.ps1 in directory with psake.psm1 or in directory with current build script

$psake.build_success = $false # indicates that the current build was successful
$psake.build_script_file = $null # contains a System.IO.FileInfo for the current build script
$psake.build_script_dir = "" # contains a string with fully-qualified path to current build script

LoadConfiguration

export-modulemember -function Invoke-psake, Invoke-Task, Task, Properties, Include, FormatTaskName, TaskSetup, TaskTearDown, Framework, Assert, Exec -variable psake

# SIG # Begin signature block
# MIIvuQYJKoZIhvcNAQcCoIIvqjCCL6YCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCDcKRc7cZuIvtRz
# 0+gOJXxQx793aKbAb73cwcpnKHh2BaCCE6YwggVkMIIDTKADAgECAhAGzuExvm1V
# yAf3wMf7ROYgMA0GCSqGSIb3DQEBDAUAMEwxCzAJBgNVBAYTAlVTMRcwFQYDVQQK
# Ew5EaWdpQ2VydCwgSW5jLjEkMCIGA1UEAxMbRGlnaUNlcnQgQ1MgUlNBNDA5NiBS
# b290IEc1MB4XDTIxMDExNTAwMDAwMFoXDTQ2MDExNDIzNTk1OVowTDELMAkGA1UE
# BhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMSQwIgYDVQQDExtEaWdpQ2Vy
# dCBDUyBSU0E0MDk2IFJvb3QgRzUwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIK
# AoICAQC2M3OA2GIDcBQsERw5XnyufIOGHf4mL0wkrYvqg1+pvD1b/AuYTAJHMOzi
# /uzoNFtmXr871yymJf+MWbPf6tp8KdlGUHIIHW7RGwrdH82ZifoPD3PE4ZwddTLN
# b5faKmqVsmzJCdDqC3t9FwZJme/W3uDIU9SuxnfxhrsjHLjA31n3jn3R74LmJota
# OLX/ddWy2U8J8zeIUNoRpIoUFNFTBAB982pEGP5QcDIHHKiaDjodxQofbgsmabc8
# oldwLIb6TG6VqVhDuawS1v8/7ddDF2tMzp7EkKv/+hBQmqOQV9bnjBCunxYazzUd
# f9d27YqcNacouKddIfwwN93eCBlPFcbnptqQR473lFNMjlMCvv2Z5eqG0K8DAtOb
# qpPxqyiOIAH/TPvMtylA9YekEhMFH0Nu11FQnzi0IO0XCRKPzLkZr5/NvmkR069V
# EG0XhnmWUsayAJ3lrziwNfSIa48OBD187q/N02oQSsbNhsoiPaFKXPsO/4jfXGKn
# wLke2axsfjg3/neTJcKFik+1NwZaBoEU8c6UnZmR6jJazmc9bgRmrQxPLaMu9571
# eJ33Cv1+j+NCilWWvPGfNy38nl+V/owYG/yO/UuQr9cDaBJjrOKTp6LLBOVPZM4D
# +sYUn9mL6MzUYoxr5AAsGZ8aBsYxgVT7UySar1WZup11rrjC3QIDAQABo0IwQDAd
# BgNVHQ4EFgQUaAGTsdJKQEJplEYsHFqIqSW0R08wDgYDVR0PAQH/BAQDAgGGMA8G
# A1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQEMBQADggIBAJL87rgCeRcCUX0hxUln
# p6TxqCQ46wxo6lpCa5z0c8FpSi2zNwVQQpiSngZ5LC4Gmfbv3yugzbOSAYO1oMsn
# tTwjGphJouwtmaVZQ6zSsZPWV9ccvJPWxkDhs28ZVbcT1+VDM6S1q8vawTFkDXTW
# LO3DjW7ru68ZR2FhLcD0BblveNw690JAZVORvZkNk5JUpqk3WSuby5nGvD33BITw
# lDMdD4JaOcsuRcMoGaOym5jI/DFrYI/26YYovOA8fXRdFolbaSTHEIvES7s2T9RZ
# P8OwpJGZ+C7RSgGd9YgS779aEWpZT1lrWmfzj7QTD8DYLz0ocqoZfxF9alufledf
# t5RP8T6hWv8tzJ3fJ3ePMnMcZwp28/pcsb+8Hb0MKJuyxxdnCzMPw7023Pu6Qgur
# 7YTDYtaEFqmxB2upbu7Gz+awRCnC8LNhgCqLb9IUXCWHVGTzpEzBofina+r+6jr8
# edsOj9zG88nUbN7pg6GOHSLsyTqyAHvcO6dCGn/ci6kRPY6nwCBvXQldQ0Tmj2bM
# qVsH8e+beg6zVOGU/Q4sxpPXVf1xmDW4CUr/xikoLPZSLdsUGJIn4hZ+jMrUYb6C
# h5HrmDc/v19ddz80rBs4Q6tocpkyHjoaGaWjOEwj16PnzNUqkheQC1pLvRa9+4Zq
# 4omZ7OSgVRjJowgfE+AyCHLQMIIGkDCCBHigAwIBAgIQCt4y6VCbRKo0sdrxvA7I
# czANBgkqhkiG9w0BAQsFADBMMQswCQYDVQQGEwJVUzEXMBUGA1UEChMORGlnaUNl
# cnQsIEluYy4xJDAiBgNVBAMTG0RpZ2lDZXJ0IENTIFJTQTQwOTYgUm9vdCBHNTAe
# Fw0yMTA3MTUwMDAwMDBaFw0zMTA3MTQyMzU5NTlaMFsxCzAJBgNVBAYTAlVTMRgw
# FgYDVQQKEw8uTkVUIEZvdW5kYXRpb24xMjAwBgNVBAMTKS5ORVQgRm91bmRhdGlv
# biBQcm9qZWN0cyBDb2RlIFNpZ25pbmcgQ0EyMIICIjANBgkqhkiG9w0BAQEFAAOC
# Ag8AMIICCgKCAgEAznkpxwyD+QKdO6sSwPBJD5wtJL9ncyIlhHFM8rWvU5lCCoMp
# 9TcAiXvCIIaJCeOxjXFv0maraGhSr8SANVefC74HBfDTAl/JyoWmOfBxRY/30/0q
# ivfUtoxrw91SR3Gu3eucWxxb4b+hoIpTgbKU+//cnSvi8EmBTk7ntfFkAWw/6Lov
# +nMXU+qEzm/TuCT8qWX2IffLkdXIt4UqQS8Jqjxn7cGLhjqDA9w+5zXpSxSu/JhK
# OecY05XcdGlGnQBPc8RBzUD3ZzXMPoPBSFiH7UZs23iVmVXCJoU9IFaN3WSLD/jZ
# 3TXE8RxJxoY1DODwr4t6kTSQdDPrx3aPrtAcJFblh3JMP0SpZZpV8DHALVZkKKfF
# u2SOL9Wv57MJ6M/mhfyUot2vLVxVlWlplgwOhcHP7a40cVBczF/cAb+IBz+tuB1q
# wGGi4B3qnE2kpYju6xYz75hVcfFqXGmy3+NMZIF6oMJUSLUZmU7HUDCUyMgHt6SP
# 42r7vzRyPJEMXARiNwe5jI6oAWxyeX6dN4ZXiBDa1lVaVuK8yUd7ShbETPbTPaZ5
# BaV/yxcl1rqExPqKzIH+y/a6F33KXSYVGTSFcg/tSEd4vuXbBUuIf2UpPVkK+J2/
# 0J/o8sBSkF3nFZ/USwrvcMKEiINKokHvmivypLkhSfMIEismXSO6rke8ElECAwEA
# AaOCAV0wggFZMBIGA1UdEwEB/wQIMAYBAf8CAQAwHQYDVR0OBBYEFCgOTIkcmZfx
# gfCPCN5XEku8uHjPMB8GA1UdIwQYMBaAFGgBk7HSSkBCaZRGLBxaiKkltEdPMA4G
# A1UdDwEB/wQEAwIBhjATBgNVHSUEDDAKBggrBgEFBQcDAzB5BggrBgEFBQcBAQRt
# MGswJAYIKwYBBQUHMAGGGGh0dHA6Ly9vY3NwLmRpZ2ljZXJ0LmNvbTBDBggrBgEF
# BQcwAoY3aHR0cDovL2NhY2VydHMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0Q1NSU0E0
# MDk2Um9vdEc1LmNydDBFBgNVHR8EPjA8MDqgOKA2hjRodHRwOi8vY3JsMy5kaWdp
# Y2VydC5jb20vRGlnaUNlcnRDU1JTQTQwOTZSb290RzUuY3JsMBwGA1UdIAQVMBMw
# BwYFZ4EMAQMwCAYGZ4EMAQQBMA0GCSqGSIb3DQEBCwUAA4ICAQA66iJQhuBqWcn+
# rukrkzv8/2CGpyWTqoospvt+Nr2zsgl1xX97v588lngs94uqlTB90YLR4227PZQz
# HQAFEs0K0/UNUfoKPC9DZSn4xJxjPsLaK8N+l4d6oZBAb7AXglpxfk4ocrzM0KWY
# Tnaz3+lt0uGi8VgP8Wggh24FLxzxxiC5SqwZ7wfU7w1g7YugDn6xbcH4+yd6ZpCn
# MDcxYkGKqOtjM7V3bd3Rbl/PDySZ+chy/n6Q6ZNj9Oz/WFYlYOId7CMmI5SzbMWp
# qbdwvPNkrSYwFRtnRV9rwJo/q9jctfwrG9FQBkHMXiLHRQPw4oEoROk0AYCm26zv
# dEqc1pPCIEQHtXyOW+GqX2MORRdiePFfmG8b1xlIw8iBJOorlbEJA6tvOpDTb1I5
# +ph6tM4DwrMrS0LbGPJv0ah9fh26ZPta1xF0KC4/gdyqY7Bmij+X+atdrRZ0jdqc
# SHspWYc9U6SaXWKVXFwvkc0b19dkzECS7ebrPQuC0+euLpvDMzHIafzL21XHB1+g
# ncuwbt7/RknJoDbFKsx5x0qDQ6vfJmrajyNAMd5fGQdgcUHs75G+KWvg7M4RtGRq
# 6NHrXnBg1LHQlRbLDSCXbIoXkywzzksKuxxm9sn2gdz0/v4o4vPQHrxk8Mj6i23U
# 6h97uJQWVPhLWhQtcjc8MJmU2i5xlDCCB6YwggWOoAMCAQICEAwNIqLvZC64iMRP
# oBjwChswDQYJKoZIhvcNAQELBQAwWzELMAkGA1UEBhMCVVMxGDAWBgNVBAoTDy5O
# RVQgRm91bmRhdGlvbjEyMDAGA1UEAxMpLk5FVCBGb3VuZGF0aW9uIFByb2plY3Rz
# IENvZGUgU2lnbmluZyBDQTIwHhcNMjQxMTA0MDAwMDAwWhcNMjcxMTAzMjM1OTU5
# WjCB5TETMBEGCysGAQQBgjc8AgEDEwJVUzEbMBkGCysGAQQBgjc8AgECEwpXYXNo
# aW5ndG9uMR0wGwYDVQQPDBRQcml2YXRlIE9yZ2FuaXphdGlvbjEUMBIGA1UEBRML
# NjAzIDM4OSAwNjgxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAw
# DgYDVQQHEwdSZWRtb25kMSMwIQYDVQQKExpKc29uLk5FVCAoLk5FVCBGb3VuZGF0
# aW9uKTEjMCEGA1UEAxMaSnNvbi5ORVQgKC5ORVQgRm91bmRhdGlvbikwggIiMA0G
# CSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQCyn0EityVT/CRCGsHJqLeK34VVttIS
# YJUJFj7eWgaL/pDCXhGRTSRd4koEABXviSdzQemsuOIPDuPyrjJUa4dhJ8UaOZQO
# XHIfX2BnFx7HA9TOP/C2YDbIQsksysh2GNa6KMmoQZzIkIXP2RSroDhIsd9eFZUa
# haN720Lwokmy/VEMIexVI/TGsPHQlw0tTtrstCNebn9uBgEbzhpbxaurLRfpIyw9
# rHSzeq8I6QDoFMlf89nquaH9V1PieS8xY1yAZwxIY8yxfqoxBtZC/5+HyMdXQXxI
# 2oRZ7OOkYBswNqe7QyDUQn+1vbQhRr3+Sjc43gttBwDlmKPwur9Vif0W/ETRzC8x
# Frwj43gadWQHrsYcmg2zo+yfp8YZS5bTH+b006lh43ly9+y9YOXcfhaN40jinK2u
# jWJhk7fD4Xa2VGyV5r8Agms0hqUDOromNo0ZblI4TMqiyJRia40nlQAka4HgqpR7
# A5hWGC5TukQvbFUqzbONzCYrVyxc+gu1A9uh41WpzePxVXa2S+0LHnPTP2IstHlG
# 07ixLfaNYVJB7Wqw3wQ74Gywp1r6Zg0MntXZhK/TYsuszBT+TCueUChzStXq+Cpu
# iukj78J1zz7LTQzbOs1qIFo4we/WhDEE+RRt4f4ogQu9nt7UJRX4ZQ/ePjWqQn17
# 2Il0fuM5ArS+hwIDAQABo4IB2TCCAdUwHwYDVR0jBBgwFoAUKA5MiRyZl/GB8I8I
# 3lcSS7y4eM8wHQYDVR0OBBYEFBMUCEWiVUypgQ8exHsjFSaMrW/TMD0GA1UdIAQ2
# MDQwMgYFZ4EMAQMwKTAnBggrBgEFBQcCARYbaHR0cDovL3d3dy5kaWdpY2VydC5j
# b20vQ1BTMA4GA1UdDwEB/wQEAwIHgDATBgNVHSUEDDAKBggrBgEFBQcDAzCBmwYD
# VR0fBIGTMIGQMEagRKBChkBodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vTkVURm91
# bmRhdGlvblByb2plY3RzQ29kZVNpZ25pbmdDQTIuY3JsMEagRKBChkBodHRwOi8v
# Y3JsNC5kaWdpY2VydC5jb20vTkVURm91bmRhdGlvblByb2plY3RzQ29kZVNpZ25p
# bmdDQTIuY3JsMIGFBggrBgEFBQcBAQR5MHcwJAYIKwYBBQUHMAGGGGh0dHA6Ly9v
# Y3NwLmRpZ2ljZXJ0LmNvbTBPBggrBgEFBQcwAoZDaHR0cDovL2NhY2VydHMuZGln
# aWNlcnQuY29tL05FVEZvdW5kYXRpb25Qcm9qZWN0c0NvZGVTaWduaW5nQ0EyLmNy
# dDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEBCwUAA4ICAQDLb+WYnliLl0sF+vux1FCn
# 08ZiVqUpViClF8q7uC+2IdrA2h2BUpxtFhp728A28O445z/9VakvSQJIwm0tWDJP
# 0mv40JamjQe2IyAMpbZB2KKomovaa0FxW56vINPFtmDK5bSB5m8qbdx/3TR/jyUz
# 50Pmdgs4uSAwYxYZPYBo4PRunsPUCLksNPmFtE8udlN6b4608egtKyL7ajvCsA7A
# 2sQucY7hVzE6gY1EtoA3gYD/THsWoK0a1zARscamio8b+oHkUjzmQM2vI5Vk6A+v
# 9ckTFP5oXBinIzZZ0UTD/5OPd7LXc3vgT1qMhNjsf4N9MrrIHN2e78zMQZD4T/cy
# VNoPQcbxe6XXjCTImROkIZI0lRedLf0WpkUrvcVJxyTnfDs40Q4e9Y5HfiaJpPH7
# UgdlLRDhCBKkGbnVSb3s6bvD0ljom5PKn/n6Wzs2jFkopV7M3aaEzLSwaC/2TVav
# FQjuAfoBrmEAOXOeezyk1/0uooHsVZ1svHUg0Wkuqq1VGe/dHnF2ULDq3QqzAgne
# ei6hByq267+SrXhJNZru5MT7PYosLQT0wJ8U+jV3gdGOMH13GYghKN0h9YrVdJ1L
# EbdZ1TzpGd5KYLO3Dhi87w+XuUgM/Kc5xu2rSPNITpctQtTHPyb++CoK7yDSD3vv
# f5m9j0BmTNQhwMTQ7sq0jjGCG2kwghtlAgEBMG8wWzELMAkGA1UEBhMCVVMxGDAW
# BgNVBAoTDy5ORVQgRm91bmRhdGlvbjEyMDAGA1UEAxMpLk5FVCBGb3VuZGF0aW9u
# IFByb2plY3RzIENvZGUgU2lnbmluZyBDQTICEAwNIqLvZC64iMRPoBjwChswDQYJ
# YIZIAWUDBAIBBQCggbQwGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQwHAYKKwYB
# BAGCNwIBCzEOMAwGCisGAQQBgjcCARUwLwYJKoZIhvcNAQkEMSIEIBkaRo0NkSWP
# oRtNFFrFzztM16i1ei1myx1hChkgzrtvMEgGCisGAQQBgjcCAQwxOjA4oBKAEABK
# AHMAbwBuAC4ATgBFAFShIoAgaHR0cHM6Ly93d3cubmV3dG9uc29mdC5jb20vanNv
# biAwDQYJKoZIhvcNAQEBBQAEggIAklJ9qEEjt2wh+lyH6FvG1msdstO7Cf9EavSf
# /ES3DG/U0b1Ewom2H91Sa1qMtmgpmykghvfdVw104Wiq5bqs6k82A6DEASKpqc7A
# +QDfog+O2VShyctgNXiMohMQguJA98oFCkLzG9aVCSAIoTXaZc1pj3ZAGR0r0dYI
# am85ITDC2G0gGZlTRySyojaTYLDBVCsHDSOzTUkAufqnM+g/lAWsskZGw3DMGZC2
# uKEuIVwX5BJS7Gpf8l8f23DFfCNdzI80d7CFjlod30OL79+6Ih3q3ky03qsQrSIV
# AryfxPUPCGiwfnbiLIH6RTH9lk1kwcUS/Ggk386PvCp3EvleesmsjNoWkVJa798h
# pTcW86jO71llxUgi+RZ+MG0fPLGvPoML6dSb0hi6lsoPRJNP/ngvt+GvsxDNNBED
# pOnmg/aJwZ3eZKmVHgd8FnmqHLaZXJy4puwPYUt2h6frlFTqZCKTwl7zpI/tDFQw
# u7bFQ/4FqSlcvAEMjoV1NuFhSwfLPLK4MSeL7G3O5HpjFnHA/hX1n2SeWw6TdlmZ
# TNXqYloZtleUPX5ZWTkq1CWOP55BKx6581ZRaRS21eHBGX3PIijgXbJ/hk+3dbod
# g69QscajuI70F2UuFtPP9xDObL/32jnakM2IzccJgyD3+z/sDMvQCNXi0IvshD15
# fi8JZw+hghgUMIIYEAYKKwYBBAGCNwMDATGCGAAwghf8BgkqhkiG9w0BBwKgghft
# MIIX6QIBAzEPMA0GCWCGSAFlAwQCAQUAMIIBYgYLKoZIhvcNAQkQAQSgggFRBIIB
# TTCCAUkCAQEGCisGAQQBhFkKAwEwMTANBglghkgBZQMEAgEFAAQgmfmjFAC2BkY0
# gXlZtRlOZORDuNSFEkvsTeYZT/UZuN0CBmjB9vpsfBgTMjAyNTA5MTYwODA0MzUu
# ODIzWjAEgAIB9KCB4aSB3jCB2zELMAkGA1UEBhMCVVMxEzARBgNVBAgTCldhc2hp
# bmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jw
# b3JhdGlvbjElMCMGA1UECxMcTWljcm9zb2Z0IEFtZXJpY2EgT3BlcmF0aW9uczEn
# MCUGA1UECxMeblNoaWVsZCBUU1MgRVNOOkE1MDAtMDVFMC1EOTQ3MTUwMwYDVQQD
# EyxNaWNyb3NvZnQgUHVibGljIFJTQSBUaW1lIFN0YW1waW5nIEF1dGhvcml0eaCC
# DyEwggeCMIIFaqADAgECAhMzAAAABeXPD/9mLsmHAAAAAAAFMA0GCSqGSIb3DQEB
# DAUAMHcxCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
# b24xSDBGBgNVBAMTP01pY3Jvc29mdCBJZGVudGl0eSBWZXJpZmljYXRpb24gUm9v
# dCBDZXJ0aWZpY2F0ZSBBdXRob3JpdHkgMjAyMDAeFw0yMDExMTkyMDMyMzFaFw0z
# NTExMTkyMDQyMzFaMGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQg
# Q29ycG9yYXRpb24xMjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVz
# dGFtcGluZyBDQSAyMDIwMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEA
# nnznUmP94MWfBX1jtQYioxwe1+eXM9ETBb1lRkd3kcFdcG9/sqtDlwxKoVIcaqDb
# +omFio5DHC4RBcbyQHjXCwMk/l3TOYtgoBjxnG/eViS4sOx8y4gSq8Zg49REAf5h
# uXhIkQRKe3Qxs8Sgp02KHAznEa/Ssah8nWo5hJM1xznkRsFPu6rfDHeZeG1Wa1wI
# SvlkpOQooTULFm809Z0ZYlQ8Lp7i5F9YciFlyAKwn6yjN/kR4fkquUWfGmMopNq/
# B8U/pdoZkZZQbxNlqJOiBGgCWpx69uKqKhTPVi3gVErnc/qi+dR8A2MiAz0kN0nh
# 7SqINGbmw5OIRC0EsZ31WF3Uxp3GgZwetEKxLms73KG/Z+MkeuaVDQQheangOEMG
# J4pQZH55ngI0Tdy1bi69INBV5Kn2HVJo9XxRYR/JPGAaM6xGl57Ei95HUw9NV/uC
# 3yFjrhc087qLJQawSC3xzY/EXzsT4I7sDbxOmM2rl4uKK6eEpurRduOQ2hTkmG1h
# SuWYBunFGNv21Kt4N20AKmbeuSnGnsBCd2cjRKG79+TX+sTehawOoxfeOO/jR7wo
# 3liwkGdzPJYHgnJ54UxbckF914AqHOiEV7xTnD1a69w/UTxwjEugpIPMIIE67SFZ
# 2PMo27xjlLAHWW3l1CEAFjLNHd3EQ79PUr8FUXetXr0CAwEAAaOCAhswggIXMA4G
# A1UdDwEB/wQEAwIBhjAQBgkrBgEEAYI3FQEEAwIBADAdBgNVHQ4EFgQUa2koOjUv
# SGNAz3vYr0npPtk92yEwVAYDVR0gBE0wSzBJBgRVHSAAMEEwPwYIKwYBBQUHAgEW
# M2h0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2lvcHMvRG9jcy9SZXBvc2l0b3J5
# Lmh0bTATBgNVHSUEDDAKBggrBgEFBQcDCDAZBgkrBgEEAYI3FAIEDB4KAFMAdQBi
# AEMAQTAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFMh+0mqFKhvKGZgEByfP
# UBBPaKiiMIGEBgNVHR8EfTB7MHmgd6B1hnNodHRwOi8vd3d3Lm1pY3Jvc29mdC5j
# b20vcGtpb3BzL2NybC9NaWNyb3NvZnQlMjBJZGVudGl0eSUyMFZlcmlmaWNhdGlv
# biUyMFJvb3QlMjBDZXJ0aWZpY2F0ZSUyMEF1dGhvcml0eSUyMDIwMjAuY3JsMIGU
# BggrBgEFBQcBAQSBhzCBhDCBgQYIKwYBBQUHMAKGdWh0dHA6Ly93d3cubWljcm9z
# b2Z0LmNvbS9wa2lvcHMvY2VydHMvTWljcm9zb2Z0JTIwSWRlbnRpdHklMjBWZXJp
# ZmljYXRpb24lMjBSb290JTIwQ2VydGlmaWNhdGUlMjBBdXRob3JpdHklMjAyMDIw
# LmNydDANBgkqhkiG9w0BAQwFAAOCAgEAX4h2x35ttVoVdedMeGj6TuHYRJklFaW4
# sTQ5r+k77iB79cSLNe+GzRjv4pVjJviceW6AF6ycWoEYR0LYhaa0ozJLU5Yi+LCm
# crdovkl53DNt4EXs87KDogYb9eGEndSpZ5ZM74LNvVzY0/nPISHz0Xva71QjD4h+
# 8z2XMOZzY7YQ0Psw+etyNZ1CesufU211rLslLKsO8F2aBs2cIo1k+aHOhrw9xw6J
# CWONNboZ497mwYW5EfN0W3zL5s3ad4Xtm7yFM7Ujrhc0aqy3xL7D5FR2J7x9cLWM
# q7eb0oYioXhqV2tgFqbKHeDick+P8tHYIFovIP7YG4ZkJWag1H91KlELGWi3SLv1
# 0o4KGag42pswjybTi4toQcC/irAodDW8HNtX+cbz0sMptFJK+KObAnDFHEsukxD+
# 7jFfEV9Hh/+CSxKRsmnuiovCWIOb+H7DRon9TlxydiFhvu88o0w35JkNbJxTk4Mh
# F/KgaXn0GxdH8elEa2Imq45gaa8D+mTm8LWVydt4ytxYP/bqjN49D9NZ81coE6aQ
# Wm88TwIf4R4YZbOpMKN0CyejaPNN41LGXHeCUMYmBx3PkP8ADHD1J2Cr/6tjuOOC
# ztfp+o9Nc+ZoIAkpUcA/X2gSMkgHAPUvIdtoSAHEUKiBhI6JQivRepyvWcl+JYbY
# bBh7pmgAXVswggeXMIIFf6ADAgECAhMzAAAASFV3ch50krf3AAAAAABIMA0GCSqG
# SIb3DQEBDAUAMGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29y
# cG9yYXRpb24xMjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFt
# cGluZyBDQSAyMDIwMB4XDTI0MTEyNjE4NDg1MloXDTI1MTExOTE4NDg1Mlowgdsx
# CzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRt
# b25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJTAjBgNVBAsTHE1p
# Y3Jvc29mdCBBbWVyaWNhIE9wZXJhdGlvbnMxJzAlBgNVBAsTHm5TaGllbGQgVFNT
# IEVTTjpBNTAwLTA1RTAtRDk0NzE1MDMGA1UEAxMsTWljcm9zb2Z0IFB1YmxpYyBS
# U0EgVGltZSBTdGFtcGluZyBBdXRob3JpdHkwggIiMA0GCSqGSIb3DQEBAQUAA4IC
# DwAwggIKAoICAQDLfoD3Z++SVTIYJFnFnPrVlMvaJYlPTronDHe0VuiHANnCKTIq
# 8qJk4weZ+cf1+vIJ7cdl+/gw3AaRgAQT/iDU6vLN6QfFg1YAO6cR7voo2y4QDJPg
# uGjKpGtONxGj9fOavAkDTH4gaTJnuK9mhvIzUqI7TEDV7JoK6Sy0kYsVcWbp2mF4
# RJ4FliqEm70YNSwLjnKn5qYIZJoQYKg9ZWYzYabgr9clHsjlZtFepsTYn2hrim8v
# aeO9dymfk7pmXrQX2O85UQl8k6AK2B8KKQVuNNnBa37EAWfxxqlO97WOvkzboNZY
# WHWFOlS3aklvSa+742PSVIyEgraCgkqIMZkVuzF+5QnuyVekXaZ/hz+3ujmyrxsn
# XUXbXYmQi6enT7comWGpTfRo2WZt+tEzvhl46YmQ9IGREfn+ZRBWr8CHA+x2q1uq
# g9GTfNUvkQ4HxLSeu4eqDFKj9ViIhQu+Yn/IGitWjufmfBKp2nigC4FFabRe4vSh
# rA7xJtrbOFmJ3jAIRtvu2dufiI7VuGQCPN2bXRjiafbBXevEuhA3998ECz4uwnGf
# SFF1u+LS7yDZLb8NzxXnuiN4bP/Xw3AjKBCGr/lnmSJiCwoMERhXCyLb8KUhAOzX
# F06EZN0xnwud2A94OTQ7o66oXbii21Z6KxjnSGV1XizJNCa+P1yFEBqVKQIDAQAB
# o4IByzCCAccwHQYDVR0OBBYEFKa9d/S6631KGfe8umYaOzc8HPdHMB8GA1UdIwQY
# MBaAFGtpKDo1L0hjQM972K9J6T7ZPdshMGwGA1UdHwRlMGMwYaBfoF2GW2h0dHA6
# Ly93d3cubWljcm9zb2Z0LmNvbS9wa2lvcHMvY3JsL01pY3Jvc29mdCUyMFB1Ymxp
# YyUyMFJTQSUyMFRpbWVzdGFtcGluZyUyMENBJTIwMjAyMC5jcmwweQYIKwYBBQUH
# AQEEbTBrMGkGCCsGAQUFBzAChl1odHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtp
# b3BzL2NlcnRzL01pY3Jvc29mdCUyMFB1YmxpYyUyMFJTQSUyMFRpbWVzdGFtcGlu
# ZyUyMENBJTIwMjAyMC5jcnQwDAYDVR0TAQH/BAIwADAWBgNVHSUBAf8EDDAKBggr
# BgEFBQcDCDAOBgNVHQ8BAf8EBAMCB4AwZgYDVR0gBF8wXTBRBgwrBgEEAYI3TIN9
# AQEwQTA/BggrBgEFBQcCARYzaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9w
# cy9Eb2NzL1JlcG9zaXRvcnkuaHRtMAgGBmeBDAEEAjANBgkqhkiG9w0BAQwFAAOC
# AgEATa2L4B40TANMMYgCNXTy+cuKTjDzNZ3dAJ+S4PbAKf78FBwQ79hYihqZ/qIg
# 6GWt/jQ5GAsBSpBYKNZOMtUMArNQfIlZ42y2tylAP/xBGQ6wwmu0uBmXzg6W3Tom
# TZ56bh90li7ZO4BbiiCg2CAkpvtTvrgYu7FbvvTqTIv/LvXQaCJx+sxvJPsbIAyW
# USfIYTdAWlVo63sJ8AkH5pzpifvkLyXmLxq2jTywaeD/pKazEJwXAby8+u04oCGV
# CZDbD+sDOJ753hbl6XyWOXmCpXVvj2wPoXJdI+T6DPtc9GWtMxSDUKZtVJV2UVgA
# Cazx8gODidj6h3aGwOr8Ut/FsO/X853Q1CYpfHWfW3JEkLc3FslKf2Kl2zH14EBo
# LeUpTykhn8NZUeXhHsuuKjPx8mUALW/LglUjZXyJ3yBQ1PiOevpxTot8afXc6rlq
# 9FJ2kgtM6ij2uW7f9at5yIcdwFM9VUm0aCgiXvjvRkQeSUIIAm40LX2qve2kdPgN
# e/Zt8yb5zDcsJjHhZPtXiW3TnBUYLqCsLnD6fVh6X5QvFbtjLlBIMt3XlvAQnuVE
# zhoyt3isww9w8t+oGCg4aNh94IdKvUNS1ffxC+Q+XrsT3wDlSlqNSLfooxhsCu5g
# XKtzpfhx8+4l9rVHJxgZE9nwGKiAbwNXxKFB3bVgmwodJbUxggdGMIIHQgIBATB4
# MGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24x
# MjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFtcGluZyBDQSAy
# MDIwAhMzAAAASFV3ch50krf3AAAAAABIMA0GCWCGSAFlAwQCAQUAoIIEnzARBgsq
# hkiG9w0BCRACDzECBQAwGgYJKoZIhvcNAQkDMQ0GCyqGSIb3DQEJEAEEMBwGCSqG
# SIb3DQEJBTEPFw0yNTA5MTYwODA0MzVaMC8GCSqGSIb3DQEJBDEiBCAcJBHqpclv
# Mz4PbWX05VJ5e8E31KcaT3TA6+nAvK5/bTCBuQYLKoZIhvcNAQkQAi8xgakwgaYw
# gaMwgaAEIOoqAVebTwjWn0P0gLwZ03YfjX3QvDtHZEl38m8i8x1BMHwwZaRjMGEx
# CzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xMjAw
# BgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFtcGluZyBDQSAyMDIw
# AhMzAAAASFV3ch50krf3AAAAAABIMIIDYQYLKoZIhvcNAQkQAhIxggNQMIIDTKGC
# A0gwggNEMIICLAIBATCCAQmhgeGkgd4wgdsxCzAJBgNVBAYTAlVTMRMwEQYDVQQI
# EwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3Nv
# ZnQgQ29ycG9yYXRpb24xJTAjBgNVBAsTHE1pY3Jvc29mdCBBbWVyaWNhIE9wZXJh
# dGlvbnMxJzAlBgNVBAsTHm5TaGllbGQgVFNTIEVTTjpBNTAwLTA1RTAtRDk0NzE1
# MDMGA1UEAxMsTWljcm9zb2Z0IFB1YmxpYyBSU0EgVGltZSBTdGFtcGluZyBBdXRo
# b3JpdHmiIwoBATAHBgUrDgMCGgMVAOYSfUGUVzjpxDh59/qJiDRZaMMnoGcwZaRj
# MGExCzAJBgNVBAYTAlVTMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24x
# MjAwBgNVBAMTKU1pY3Jvc29mdCBQdWJsaWMgUlNBIFRpbWVzdGFtcGluZyBDQSAy
# MDIwMA0GCSqGSIb3DQEBCwUAAgUA7HMM+zAiGA8yMDI1MDkxNTIyMDg1OVoYDzIw
# MjUwOTE2MjIwODU5WjB3MD0GCisGAQQBhFkKBAExLzAtMAoCBQDscwz7AgEAMAoC
# AQACAj0yAgH/MAcCAQACAhM5MAoCBQDsdF57AgEAMDYGCisGAQQBhFkKBAIxKDAm
# MAwGCisGAQQBhFkKAwKgCjAIAgEAAgMHoSChCjAIAgEAAgMBhqAwDQYJKoZIhvcN
# AQELBQADggEBAKVPgFxnjYhxu+7pjTWSUocvUrGjdGYxwEaZr/TkIW9uSAoY5izw
# Te64iMYm5J1B1rIJPV/hHeP4aiRJlveO17guPDWQ7IzMQ4chNQpEhyA6ariCjryY
# Wo4zN1eHkpS9TVpevNjSGS8XMT3oHm7o5BxGoDmZPgSxTqTJyp4U9kTZF/njboIv
# 084q7AnGgbuEGf8EUl7iOSfCcximrlie4xjDxkv9vTZQYziwi4hOykh2sToER1+Z
# bGNQKg/WC/BgRdiT3QHc7kjlKrIaF3WHV16RISVUpBS44pi3HX32OgdoG8xqgXz7
# 1YD1ucfmSQpYIZBuASQ7bOoRUW9kZJ2gV+cwDQYJKoZIhvcNAQEBBQAEggIAHg87
# Pz1EjHbJsVBneQeg3u/5WSarlB+S+m3u7nxalyDVTSrVQAY54hKPx4NP+JxmFKaq
# NnprmkCW2KOG5aOyu8VRdtAa4cbSTrTChTKtXuLkZMCi/DOzzSV/r8kHnlqpM6fa
# 6r71Fsafu9mxqRtqhrWJdeSgDqfEZrDsbSosBmikkuqjaHo77krUNJ3w/lqwl42B
# 6jpa/1KnUIgJKa/DHostAN1How6/YI6ORAM3KdrZIVCyBqcYYsyogSNAZNMlfI8r
# axXn40oTFWcRLw6nmXzCvnqPD2N0um0lzlPg/4s42HfG4NdHu7J2tD1dmuAyYkEg
# J+isqND5MZhyHBAUwQrbGAheVicMPsbCfCZhXV9Nh6RvQ0+6QW9eq3X7UqmODx2a
# 3U/HQOKB3WhrunEm3wyDvIpLDB7m/9pWB4w3g+jRxjEDWZg4LyWvg9eWrGYZXLYH
# aT81NoLdzQWSc3LEgCDgZyhsmJ6nSoVisteiijI3PadIr7xcJ7xU6BjWD6R+RvtU
# dV0k05wKV2L8KlAvEIq6Dk7YQvrl009zMUmvPAYjfqPUE6sxlbSjpRaa0sRC14Sx
# U7UZL/F47vVasWiRukX5sRrv//ZJh5LhU4l2RBzOLjeb57UFk6oPmBXTNV3FeuPt
# WTOimlrJYeoMnI+Y1EXnvJYoKjvQ20LZIh5ObR0=
# SIG # End signature block
