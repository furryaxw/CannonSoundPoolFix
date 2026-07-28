$sourcePath = Join-Path $PSScriptRoot `
    '..\CannonSoundPoolFix\CannonSoundPoolFixMod.cs'
$source = Get-Content -Raw -LiteralPath $sourcePath
$soundPolicyPath = Join-Path $PSScriptRoot `
    '..\CannonSoundPoolFix\SoundPoolRetentionPolicy.cs'
$soundPolicy = Get-Content -Raw -LiteralPath $soundPolicyPath
$effectPolicyPath = Join-Path $PSScriptRoot `
    '..\CannonSoundPoolFix\EffectPoolPolicy.cs'
$effectPolicy = Get-Content -Raw -LiteralPath $effectPolicyPath

if ($source -match 'SetActive\s*\(' -or
    $source -match 'FindDirectSfxChild' -or
    $source -match 'GetComponentsInChildren') {
    throw 'Runtime contract failed: hierarchy or GameObject mutation remains.'
}

$stopCalls = [regex]::Matches(
    $source,
    'source\.Stop\s*\(\s*\)')
if ($stopCalls.Count -ne 1 -or
    $source -notmatch
        'StopAudioSource\s*\(\s*effect\.AudioSource\s*\)' -or
    $source -notmatch
        'StopAudioSource\s*\(\s*effect\.LongRangeAudioSource\s*\)') {
    throw 'Runtime contract failed: both cannon AudioSources must be stopped.'
}

if ($source -match '\.(?:visualEffect|Vfx|VFX|Light)\s*=' -or
    $source -match '(?:visualEffect|Vfx|VFX|Light)\.Stop') {
    throw 'Runtime contract failed: VFX or Light mutation detected.'
}

if ($source -notmatch 'SoundPoolRetentionPolicy\.SelectSourcesToStop' -or
    $soundPolicy -notmatch 'MaxPlayingEffectsPerArea\s*=\s*5' -or
    $soundPolicy -notmatch 'SuppressionBoxSize\s*=\s*1\.0f') {
    throw 'Runtime contract failed: newest-five spatial policy is missing.'
}

if ($source -notmatch 'prototype\.MaxInstanceCount\s*=\s*configuredLimit' -or
    $effectPolicy -notmatch 'DefaultMuzzleEffectLimit\s*=\s*192') {
    throw 'Runtime contract failed: root Effect pool bound is missing.'
}

if ($source -match '\[DEBUG-' -or
    $source -match 'GameAudioEventQueue' -or
    $source -match 'LogPoolDiagnostic') {
    throw 'Runtime contract failed: temporary diagnostics remain.'
}

Write-Output `
    'CannonSoundPoolFix runtime contract passed: dual AudioSource stop plus bounded Effect pooling'
