# Merge VC5 card registry, fix invalid asset format, bind placeholder abilities.
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path "$Root/Assets")) { throw "Project root not found: $Root" }

$CardsVc5 = Join-Path $Root "Assets/TcgEngine/Resources/Cards/vc5"
$AbilitiesDir = Join-Path $Root "Assets/TcgEngine/Resources/Abilities/vc5/cards"
$RegistryDir = Join-Path $Root "Docs/vc5_card_registry"
$CardScriptGuid = "843a21f7f6f205741a0a7341aec8e84d"
$AbilityScriptGuid = "8ce5f81e5cc37f547af6923758602c8c"
$RarityGuid = "fab1a52f5a36cc942985dea95be18ac9"
$EffectDamageGuid = "4369324687c62ca488c57afd73a2be36"
$EffectHealGuid = "5e489970bbb948c409125b3ab22f2aac"
$EffectDrawGuid = "d2f15281b01c4b347a527a3011ef430a"
$CondEnemy1 = "ef09540f94d8428408b69fea46ec3334"
$CondEnemy2 = "a1fb3a7171663234280fdfb41c99ab0a"
$TrapCond1 = "065da32cbd133734493695579723a718"
$TrapCond2 = "41c58513071a53b4e9066a58c439760f"

$TypeNames = @{0="None";5="Hero";10="Character";20="Spell";30="Artifact";40="Secret";50="Equipment"}

function New-Guid32 { return ([guid]::NewGuid().ToString("N")).ToLower() }

function Read-Field($text, $name, $default = "") {
    if ($text -match "(?m)^\s*$([regex]::Escape($name)):\s*(.*)$") { return $Matches[1].Trim().Trim('"') }
    return $default
}

function Load-SupplementalCsv($path) {
    $map = @{}
    if (-not (Test-Path $path)) { return $map }
    Import-Csv $path | ForEach-Object {
        $row = $_
        if ($row.id) { $map[$row.id] = $row }
        if ($row.asset_path) {
            $base = [IO.Path]::GetFileNameWithoutExtension($row.asset_path)
            if ($base) { $map[$base] = $row }
        }
    }
    return $map
}

function Get-Supplemental($map, $id, $fileName) {
    if ($map.ContainsKey($id)) { return $map[$id] }
    if ($map.ContainsKey($fileName)) { return $map[$fileName] }
    $heroKey = "hero_$id"
    if ($map.ContainsKey($heroKey)) { return $map[$heroKey] }
    return $null
}

function Convert-ToCardDataAsset($path, $supp) {
    $text = Get-Content $path -Raw -Encoding UTF8
    if ($text -match "guid: $CardScriptGuid") { return $false }

    $id = Read-Field $text "id"
    $title = Read-Field $text "title"
    $typeNum = [int](Read-Field $text "type" "20")
    $mana = Read-Field $text "mana" "0"
    $attack = Read-Field $text "attack" "0"
    $hp = Read-Field $text "hp" "0"
    $move = Read-Field $text "move_Range" (Read-Field $text "move_range" "2")
    $attackRange = Read-Field $text "attack_Range" (Read-Field $text "attack_range" "2")
    $series = if ($supp -and $supp.series) { $supp.series } else { Read-Field $text "archetype" "vc5" }
    $cardText = if ($supp -and $supp.text) { $supp.text } elseif ($supp -and $supp.notes) { $supp.notes } else { $title }
    $isHeroFile = [IO.Path]::GetFileName($path) -like "hero_vc5_*"
    $deckbuilding = if ($isHeroFile) { "0" } else { "1" }
    $name = [IO.Path]::GetFileNameWithoutExtension($path)

    $yaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $CardScriptGuid, type: 3}
  m_Name: $name
  m_EditorClassIdentifier: 
  id: $id
  title: $title
  series: $series
  art_full: {fileID: 0}
  art_board: {fileID: 0}
  type: $typeNum
  team: {fileID: 0}
  rarity: {fileID: 11400000, guid: $RarityGuid, type: 2}
  mana: $mana
  attack: $attack
  hp: $hp
  move_Range: $move
  attack_Range: $attackRange
  traits: []
  stats: []
  abilities: []
  text: $cardText
  desc: 
  spawn_fx: {fileID: 0}
  death_fx: {fileID: 0}
  attack_fx: {fileID: 0}
  damage_fx: {fileID: 0}
  idle_fx: {fileID: 0}
  spawn_audio: {fileID: 0}
  death_audio: {fileID: 0}
  attack_audio: {fileID: 0}
  damage_audio: {fileID: 0}
  deckbuilding: $deckbuilding
  cost: 100
  packs: []
"@
    [IO.File]::WriteAllText($path, $yaml, [Text.UTF8Encoding]::new($false))
    return $true
}

function New-AbilityAsset($abilityPath, $abilityId, $trigger, $target, $effectGuid, $value, $title, $desc, $conditions) {
    $guid = New-Guid32
    if ($conditions -and $conditions.Count -gt 0) {
        $condLines = ($conditions | ForEach-Object { "  - {fileID: 11400000, guid: $_, type: 2}" }) -join "`n"
    } else {
        $condLines = "  []"
    }

    $yaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $AbilityScriptGuid, type: 3}
  m_Name: $abilityId
  m_EditorClassIdentifier: 
  id: $abilityId
  trigger: $trigger
  conditions_trigger: []
  target: $target
  conditions_target:
$condLines
  filters_target: []
  effects:
  - {fileID: 11400000, guid: $effectGuid, type: 2}
  status: []
  value: $value
  duration: 0
  chain_abilities: []
  mana_cost: 0
  exhaust: 0
  board_fx: {fileID: 0}
  caster_fx: {fileID: 0}
  target_fx: {fileID: 0}
  projectile_fx: {fileID: 0}
  cast_audio: {fileID: 0}
  target_audio: {fileID: 0}
  charge_target: 0
  title: $title
  desc: $desc
"@
    [IO.File]::WriteAllText($abilityPath, $yaml, [Text.UTF8Encoding]::new($false))
    $meta = @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    [IO.File]::WriteAllText("$abilityPath.meta", $meta, [Text.UTF8Encoding]::new($false))
    return $guid
}

function Bind-AbilityToCard($cardPath, $abilityGuid) {
    $text = Get-Content $cardPath -Raw -Encoding UTF8
    if ($text -match "abilities:\s*\n\s*- \{fileID:") { return $false }
    $replacement = "abilities:`n  - {fileID: 11400000, guid: $abilityGuid, type: 2}"
    $text = [regex]::Replace($text, "abilities:\s*\[\]", $replacement)
    [IO.File]::WriteAllText($cardPath, $text, [Text.UTF8Encoding]::new($false))
    return $true
}

function Pick-Effect($id, $typeNum, $mana) {
    $v = [Math]::Max(1, [int]$mana)
    if ($id -match "heal|recover|heart|meditate|calm|repair|fix") { return @{g=$EffectHealGuid; v=$v} }
    if ($id -match "draw|wisdom|awaken|collect") { return @{g=$EffectDrawGuid; v=1} }
    return @{g=$EffectDamageGuid; v=$v}
}

New-Item -ItemType Directory -Force -Path $AbilitiesDir | Out-Null
$supp = @{}
foreach ($k in (Load-SupplementalCsv (Join-Path $RegistryDir "cards_registry_new_entries.csv")).Keys) {
    $supp[$k] = (Load-SupplementalCsv (Join-Path $RegistryDir "cards_registry_new_entries.csv"))[$k]
}
$fam = Load-SupplementalCsv (Join-Path $RegistryDir "cards_registry_new_families.csv")
foreach ($k in $fam.Keys) { $supp[$k] = $fam[$k] }

$converted = 0
$bound = 0
$registryRows = @()

Get-ChildItem $CardsVc5 -Filter "*.asset" -Recurse | Where-Object { $_.Name -match '^(card_vc5_|hero_vc5_)' } | ForEach-Object {
    $path = $_.FullName
    $fileName = $_.BaseName
    $rel = $path.Substring($Root.Length + 1).Replace("\", "/")
    $text = Get-Content $path -Raw -Encoding UTF8
    $sup = Get-Supplemental $supp (Read-Field $text "id") $fileName

    if (Convert-ToCardDataAsset $path $sup) { $converted++ }
    $text = Get-Content $path -Raw -Encoding UTF8

    $id = Read-Field $text "id"
    $title = Read-Field $text "title"
    $typeNum = [int](Read-Field $text "type" "20")
    $typeName = $TypeNames[[int]$typeNum]
    if ($sup -and $sup.type) {
        switch ($sup.type.ToLower()) {
            "hero" { $typeName = "Character" }
            "spell" { $typeName = "Spell" }
            "secret" { $typeName = "Secret" }
            "skill" { $typeName = "Spell" }
        }
    }

    $cardText = Read-Field $text "text" $title
    $series = Read-Field $text "series" "vc5"
    $isHeroFile = $fileName -like "hero_vc5_*"

    if ($text -match "abilities:\s*\[\]") {
        $abilityId = "${id}_ability"
        $abilityPath = Join-Path $AbilitiesDir "$abilityId.asset"
        $fx = Pick-Effect $id $typeNum (Read-Field $text "mana" "1")
        if ($typeNum -eq 40) {
            $abGuid = New-AbilityAsset $abilityPath $abilityId 30 10 $fx.g $fx.v $title $cardText @($TrapCond1, $TrapCond2)
        } elseif ($isHeroFile -or $typeNum -eq 10) {
            $abGuid = New-AbilityAsset $abilityPath $abilityId 5 30 $fx.g $fx.v $title $cardText @()
        } else {
            $abGuid = New-AbilityAsset $abilityPath $abilityId 10 30 $fx.g $fx.v $title $cardText @($CondEnemy1, $CondEnemy2)
        }
        if (Bind-AbilityToCard $path $abGuid) { $bound++ }
    }

    $registryRows += [PSCustomObject]@{
        enabled = "1"
        id = $id
        title = $title
        series = $series
        type = $typeName
        mana = Read-Field $text "mana" "0"
        hp_cost = "0"
        discard_cost = "0"
        attack = Read-Field $text "attack" "0"
        hp = Read-Field $text "hp" "0"
        move_Range = Read-Field $text "move_Range" "2"
        attack_Range = Read-Field $text "attack_Range" "2"
        fast_action = "0"
        deckbuilding = Read-Field $text "deckbuilding" "1"
        text = $cardText
        source = "asset"
        asset_path = $rel
        notes = if ($sup -and $sup.notes) { $sup.notes } else { "" }
        _isHero = $isHeroFile
    }
}

function Merge-Registry($existingPath, $newRows, $isHeroFile) {
    $existing = @{}
    if (Test-Path $existingPath) {
        Import-Csv $existingPath | ForEach-Object { $existing[$_.id] = $_ }
    }
    foreach ($row in $newRows) {
        if ($row._isHero -ne $isHeroFile) { continue }
        $existing[$row.id] = [PSCustomObject]@{
            enabled = $row.enabled; id = $row.id; title = $row.title; series = $row.series
            type = $row.type; mana = $row.mana; hp_cost = $row.hp_cost; discard_cost = $row.discard_cost
            attack = $row.attack; hp = $row.hp; move_Range = $row.move_Range; attack_Range = $row.attack_Range
            fast_action = $row.fast_action; deckbuilding = $row.deckbuilding; text = $row.text
            source = $row.source; asset_path = $row.asset_path; notes = $row.notes
        }
    }
    $existing.Values | Sort-Object id | Export-Csv $existingPath -NoTypeInformation -Encoding UTF8
}

Merge-Registry (Join-Path $RegistryDir "heroes_registry.csv") $registryRows $true
Merge-Registry (Join-Path $RegistryDir "cards_registry.csv") $registryRows $false

Write-Host "Converted assets: $converted"
Write-Host "Bound abilities: $bound"
Write-Host "Registry rows processed: $($registryRows.Count)"
