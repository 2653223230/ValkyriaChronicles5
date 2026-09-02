$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$ink = '#18383E'
$teal = '#168D87'
$red = '#D54A4F'
$gold = '#D69A13'
$paper = '#F5F7F7'

function N([double]$v) { $v.ToString('0.##', [Globalization.CultureInfo]::InvariantCulture) }
function Point([int]$q, [int]$r) { @((400 + 105 * $q), (386 + 121.24 * ($r + $q / 2.0))) }
function Hex([double]$x, [double]$y, [double]$size, [string]$fill, [string]$stroke, [double]$width = 4) {
    $points = for ($i = 0; $i -lt 6; $i++) {
        $angle = $i * [Math]::PI / 3
        (N ($x + $size * [Math]::Cos($angle))) + ',' + (N ($y + $size * [Math]::Sin($angle)))
    }
    '<polygon points="' + ($points -join ' ') + '" fill="' + $fill + '" stroke="' + $stroke + '" stroke-width="' + (N $width) + '"/>'
}
function Circle([double]$x, [double]$y, [double]$r, [string]$fill, [string]$stroke = 'none', [double]$width = 0) {
    "<circle cx='$(N $x)' cy='$(N $y)' r='$(N $r)' fill='$fill' stroke='$stroke' stroke-width='$(N $width)'/>"
}
function Line([double]$x1, [double]$y1, [double]$x2, [double]$y2, [string]$color, [double]$width = 6, [string]$dash = '') {
    "<path d='M$(N $x1) $(N $y1) L$(N $x2) $(N $y2)' fill='none' stroke='$color' stroke-width='$(N $width)' stroke-dasharray='$dash'/>"
}
function Txt([double]$x, [double]$y, [string]$text, [int]$size = 40, [string]$color = $ink, [string]$anchor = 'middle') {
    $safe = [Security.SecurityElement]::Escape($text)
    "<text xml:space='preserve' x='$(N $x)' y='$(N $y)' fill='$color' font-size='$size' text-anchor='$anchor' font-family='Noto Sans SC, Microsoft YaHei, sans-serif' font-weight='700'>$safe</text>"
}
function Ally($p, [bool]$ghost = $false) {
    $x = $p[0]; $y = $p[1]
    if ($ghost) {
        return "<circle cx='$(N $x)' cy='$(N $y)' r='34' fill='#DBF1EE' stroke='$teal' stroke-width='6' stroke-dasharray='8 9'/>"
    }
    (Circle $x $y 37 $teal $ink 5) + (Circle $x $y 13 '#FFFFFF')
}
function Enemy($p, [bool]$muted = $false) {
    $x = $p[0]; $y = $p[1]
    $fill = if ($muted) { '#EAB3B4' } else { $red }
    "<path d='M$(N $x) $(N ($y-39)) L$(N ($x+39)) $(N $y) L$(N $x) $(N ($y+39)) L$(N ($x-39)) $(N $y) Z' fill='$fill' stroke='$ink' stroke-width='5'/>" +
        (Line ($x-9) $y ($x+9) $y '#FFFFFF' 5)
}
function Arrow($a, $b, [string]$color = $teal, [double]$width = 12) {
    $dx = $b[0]-$a[0]; $dy = $b[1]-$a[1]
    $length = [Math]::Sqrt($dx*$dx+$dy*$dy)
    $ux = $dx/$length; $uy = $dy/$length
    $x = $b[0]; $y = $b[1]
    (Line $a[0] $a[1] ($x-12*$ux) ($y-12*$uy) $color $width) +
        "<path d='M$(N $x) $(N $y) L$(N ($x-26*$ux-15*$uy)) $(N ($y-26*$uy+15*$ux)) L$(N ($x-26*$ux+15*$uy)) $(N ($y-26*$uy-15*$ux)) Z' fill='$color'/>"
}
function Hit($p, [double]$size = 25) {
    $x = $p[0]; $y = $p[1]
    $points = for ($i = 0; $i -lt 8; $i++) {
        $r = if ($i % 2 -eq 0) { $size } else { $size * 0.3 }
        $a = $i * [Math]::PI / 4
        (N ($x+$r*[Math]::Cos($a))) + ',' + (N ($y+$r*[Math]::Sin($a)))
    }
    "<polygon points='$($points -join ' ')' fill='#FFFFFF' stroke='$red' stroke-width='4'/>"
}
function Shot($a, $b) {
    $dx=$b[0]-$a[0]; $dy=$b[1]-$a[1]; $len=[Math]::Sqrt($dx*$dx+$dy*$dy)
    $ux=$dx/$len; $uy=$dy/$len
    (Line ($a[0]+44*$ux) ($a[1]+44*$uy) ($b[0]-44*$ux) ($b[1]-44*$uy) $ink 7) +
        (Hit @(($b[0]-32*$ux), ($b[1]-32*$uy)) 22)
}
function Reticle($p) {
    $x=$p[0]; $y=$p[1]
    (Circle $x $y 57 'none' $red 6) +
        (Line ($x-75) $y ($x-48) $y $red 6) + (Line ($x+48) $y ($x+75) $y $red 6) +
        (Line $x ($y-75) $x ($y-48) $red 6) + (Line $x ($y+48) $x ($y+75) $red 6)
}
function Hp($p, [int]$filled) {
    $x=$p[0]-49; $y=$p[1]-99
    $s = "<rect x='$(N $x)' y='$(N $y)' width='98' height='24' rx='4' fill='#FFFFFF' stroke='$ink' stroke-width='4'/>"
    for($i=0; $i -lt 5; $i++) {
        $fill=if($i -lt $filled){$red}else{'#DCE3E4'}
        $s += "<rect x='$(N ($x+5+$i*18))' y='$(N ($y+5))' width='15' height='14' rx='1' fill='$fill'/>"
    }
    $s
}
function Grid([string]$mode = 'plain') {
    $s=''
    for($q=-2; $q -le 2; $q++) {
        for($r=-2; $r -le 2; $r++) {
            $distance=[Math]::Max([Math]::Abs($q),[Math]::Max([Math]::Abs($r),[Math]::Abs($q+$r)))
            if($distance -gt 2){continue}
            $fill='#EBF0F0'; $stroke='#D0DADC'; $width=3
            if($mode -eq 'range'){$fill='#DEF0ED';$stroke='#A6CBC6'}
            if($mode -eq 'expand'){
                if($distance -eq 2){$fill='#FFF1CC';$stroke=$gold;$width=5}
                else{$fill='#DEF0ED';$stroke='#83BDB6'}
            }
            $p=Point $q $r
            $s += Hex $p[0] $p[1] 67 $fill $stroke $width
        }
    }
    $s
}
function Clock([double]$x,[double]$y) {
    (Circle $x $y 29 'none' $ink 6) + (Line $x $y $x ($y-17) $ink 6) + (Line $x $y ($x+13) ($y+7) $ink 6)
}
function Upgrade([double]$x,[double]$y) {
    $s=''
    for($i=0;$i -lt 8;$i++){
        $a=$i*[Math]::PI/4
        $s+=Line ($x+25*[Math]::Cos($a)) ($y+25*[Math]::Sin($a)) ($x+34*[Math]::Cos($a)) ($y+34*[Math]::Sin($a)) $ink 12
    }
    $s+=(Circle $x $y 25 '#E4ECEC' $ink 6)
    $s+=(Line ($x-11) $y ($x+11) $y $teal 6)+(Line $x ($y-11) $x ($y+11) $teal 6)
    $s
}
function SaveArt([string]$name,[string]$body,[string]$footer) {
    $svg="<svg xmlns='http://www.w3.org/2000/svg' width='1024' height='1024' viewBox='0 0 800 800'><rect width='800' height='800' fill='$paper'/><g stroke-linecap='round' stroke-linejoin='round'><g transform='translate(0 -45)'>$body</g>$footer</g></svg>"
    [IO.File]::WriteAllText((Join-Path $root ($name+'.svg')), $svg, [Text.UTF8Encoding]::new($false))
    & magick -background $paper (Join-Path $root ($name+'.svg')) (Join-Path $root ($name+'.png'))
    if($LASTEXITCODE -ne 0){throw "SVG render failed: $name"}
}

$a=Point -1 1; $b=Point 1 -1
$body=Grid
$body+=Hex $b[0] $b[1] 61 '#D5EFEB' $teal 6
$body+=Arrow @(($a[0]+40),($a[1]-23)) @(($b[0]-44),($b[1]+25))
$body+=(Ally $a)+(Ally $b $true)
SaveArt '01_tactical_move' $body (Txt 400 715 '移动到空格' 44)

$a=Point -2 2; $mid=Point 0 0; $b=Point 2 -2
$body=Grid
$body+=Arrow @(($a[0]+44),($a[1]-25)) @(($mid[0]-12),($mid[1]+7)) $teal 12
$body+=Arrow @(($mid[0]+12),($mid[1]-7)) @(($b[0]-45),($b[1]+26)) $gold 15
$body+=(Ally $a)+(Hex $b[0] $b[1] 61 '#FFF1CC' $gold 6)+(Ally $b $true)
SaveArt '02_forced_march' $body (Txt 400 715 '移动 +2' 48)

$body=Grid 'expand'
$body+=Ally (Point 0 0)
$body+=Arrow @(487,385) @(571,385) $gold 11
$footer=(Clock 239 711)+(Txt 457 698 '射程 +1' 45)+(Txt 457 750 '本回合' 31 $teal)
SaveArt '03_temp_calibration' $body $footer

$body=Grid 'expand'
$body+=Ally (Point 0 0)
$body+=Circle 400 386 92 'none' $ink 7
$body+=(Line 400 274 400 306 $ink 7)+(Line 480 386 512 386 $ink 7)+(Line 400 466 400 498 $ink 7)+(Line 288 386 320 386 $ink 7)
$footer=(Upgrade 239 711)+(Txt 457 698 '射程 +1' 45)+(Txt 457 750 '永久' 31 $teal)
SaveArt '04_scope_upgrade' $body $footer

$a=Point 0 1; $enemies=@((Point -1 0),(Point 0 -1),(Point 1 -1))
$body=Grid 'range'
foreach($p in $enemies){$body+=Shot $a $p}
$body+=Ally $a
foreach($p in $enemies){$body+=(Enemy $p)+(Hit @(($p[0]+27),($p[1]+30)) 26)}
SaveArt '05_fire_coverage' $body (Txt 400 715 '范围内全部敌人' 44)

$a=Point 0 1; $left=Point -1 0; $target=Point 0 -1; $right=Point 1 -1
$body=Grid
$body+=Shot $a $target
$body+=(Ally $a)+(Enemy $left $true)+(Enemy $right $true)+(Enemy $target)+(Reticle $target)
$body+=(Hp $left 2)+(Hp $right 3)+(Hp $target 5)
$footer=(Txt 400 700 '锁定最高生命' 42)+(Txt 400 752 '伤害 +1' 34 $red)
SaveArt '06_heavy_break' $body $footer

$a=Point 0 0; $left=Point -1 0; $right=Point 1 0; $target=Point 0 -2
$body="<g transform='translate(20 40) scale(.95)'>"+(Grid)
$body+=Hex $a[0] $a[1] 154 'none' '#72B5AD' 5
$body+=Hex $a[0] $a[1] 300 'none' $gold 7
$body+=Shot $a $target
$body+=(Ally $a)+(Enemy $left $true)+(Enemy $right $true)+(Enemy $target)+(Reticle $target)
$body+=(Hp $left 4)+(Hp $right 5)+(Hp $target 1)
$body+='</g>'
$footer=(Txt 400 700 '锁定最低生命' 42)+(Txt 400 752 '射程 +2' 34 $gold)
SaveArt '07_weakpoint_snipe' $body $footer

$a=Point -1 1; $b=Point 1 0; $target=Point 1 -2
$body=Grid
$body+=Hex $b[0] $b[1] 61 '#DEF0ED' $teal 6
$body+=Arrow @(($a[0]+47),$a[1]) @(($b[0]-48),$b[1]) $teal 14
$body+=Shot $b $target
$body+=(Ally $a)+(Ally $b $true)+(Enemy $target)+(Hit @(($target[0]+27),($target[1]+28)) 25)
$footer=(Txt 400 700 '自动移动后射击' 42)+(Txt 400 752 '可攻击时不移动' 28 '#597176')
SaveArt '08_mobile_shot' $body $footer

$names=@('战术移动','强行军','临时校准','改装瞄具','火力覆盖','重点击破','弱点狙击','移动射击')
$files=@('01_tactical_move','02_forced_march','03_temp_calibration','04_scope_upgrade','05_fire_coverage','06_heavy_break','07_weakpoint_snipe','08_mobile_shot')
$notes=@('移动到空格','移动力 +2','射程 +1 / 本回合','射程 +1 / 永久','打范围内所有敌人','最高当前生命 / 伤害 +1','最低当前生命 / 射程 +2','自动移动与射击')

function SaveReview([bool]$small) {
    $cellW=if($small){300}else{440}
    $artSize=if($small){160}else{396}
    $cellH=if($small){280}else{486}
    $w=4*$cellW+80; $h=2*$cellH+210
    $s="<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='$w' height='$h' viewBox='0 0 $w $h'><rect width='$w' height='$h' fill='#E7ECEC'/>"
    $title=if($small){'C3 卡图 / 缩小识别检查'}else{'射击压制 C3 / 八张功能卡图'}
    $s+=Txt 42 63 $title 32 $ink 'start'
    $s+=Txt 42 105 'v1 审核稿    青色圆形：己方    红色菱形：敌方    黄色：额外距离' 20 '#597176' 'start'
    for($i=0;$i -lt 8;$i++){
        $col=$i%4; $row=[Math]::Floor($i/4)
        $cx=40+$col*$cellW+$cellW/2; $y=142+$row*$cellH
        $data=[Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $root ($files[$i]+'.png'))))
        $s+="<image x='$(N ($cx-$artSize/2))' y='$(N $y)' width='$artSize' height='$artSize' xlink:href='data:image/png;base64,$data'/>"
        $s+=Txt $cx ($y+$artSize+34) (('{0:00}' -f ($i+1))+'  '+$names[$i]) 25
        $s+=Txt $cx ($y+$artSize+65) $notes[$i] 18 '#597176'
    }
    $s+=Txt 42 ($h-22) '仅卡图审核，不含角色美术；尚未接入 Unity。图中格数与血条仅为示意。' 18 '#597176' 'start'
    $s+='</svg>'
    $file=if($small){'review-small'}else{'review-overview'}
    [IO.File]::WriteAllText((Join-Path $root ($file+'.svg')), $s, [Text.UTF8Encoding]::new($false))
    & magick -background '#E7ECEC' (Join-Path $root ($file+'.svg')) (Join-Path $root ($file+'.png'))
    if($LASTEXITCODE -ne 0){throw "Review render failed: $file"}
}
SaveReview $false
SaveReview $true
Write-Output 'Rendered eight SVG/PNG illustrations and two review sheets.'
