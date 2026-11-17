# Generates a new coordinate-accurate design diagram. Does not edit any game asset.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$canvas = New-Object System.Drawing.Bitmap 1600,1120
$gfx = [System.Drawing.Graphics]::FromImage($canvas)
$gfx.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$gfx.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$gfx.Clear([System.Drawing.ColorTranslator]::FromHtml('#F4F4E9'))
function Brush($hex) { New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($hex)) }
function Pen($hex,$width=1) { New-Object System.Drawing.Pen ([System.Drawing.ColorTranslator]::FromHtml($hex)),$width }
function Box($x,$y,$w,$h,$hex) { $b=Brush $hex; $gfx.FillRectangle($b,[single]$x,[single]$y,[single]$w,[single]$h); $b.Dispose() }
function Circle($x,$y,$radius,$hex) { $b=Brush $hex; $gfx.FillEllipse($b,[single]($x-$radius),[single]($y-$radius),[single](2*$radius),[single](2*$radius)); $b.Dispose() }
function Line($x1,$y1,$x2,$y2,$hex,$width=2) { $p=Pen $hex $width; $gfx.DrawLine($p,[single]$x1,[single]$y1,[single]$x2,[single]$y2); $p.Dispose() }
function TextAt($value,$x,$y,$size=22,$hex='#273D39',$bold=$false) {
    $style=[System.Drawing.FontStyle]::Regular
    if($bold){$style=[System.Drawing.FontStyle]::Bold}
    $f=New-Object System.Drawing.Font 'Microsoft YaHei UI',([single]$size),$style,([System.Drawing.GraphicsUnit]::Pixel)
    $b=Brush $hex
    $gfx.DrawString([string]$value,$f,$b,[single]$x,[single]$y)
    $f.Dispose(); $b.Dispose()
}
function CenterText($value,$cx,$y,$size=22,$hex='#273D39',$bold=$false) {
    $style=[System.Drawing.FontStyle]::Regular
    if($bold){$style=[System.Drawing.FontStyle]::Bold}
    $f=New-Object System.Drawing.Font 'Microsoft YaHei UI',([single]$size),$style,([System.Drawing.GraphicsUnit]::Pixel)
    $s=$gfx.MeasureString([string]$value,$f)
    $b=Brush $hex
    $gfx.DrawString([string]$value,$f,$b,[single]($cx-$s.Width/2),[single]$y)
    $f.Dispose(); $b.Dispose()
}
$left=95; $top=175; $cell=110
function CellCenter($coord) { @(($left+(([int][char]$coord[0])-65)*$cell+$cell/2),($top+(([int]$coord.Substring(1))-1)*$cell+$cell/2)) }
function Token($coord,$symbol,$name,$hp,$color) {
    $pos=CellCenter $coord; $x=$pos[0]; $y=$pos[1]
    Circle $x ($y-9) 34 '#FFFFFF'
    Circle $x ($y-9) 30 $color
    CenterText $symbol $x ($y-31) 31 '#FFFFFF' $true
    Circle ($x+29) ($y-32) 15 '#FFFFFF'
    CenterText $hp ($x+29) ($y-44) 17 $color $true
    CenterText $name $x ($y+27) 20 $color $true
}
Box 0 0 1600 12 '#315C4D'
TextAt '晴岚炮台' 90 34 47 '#234A3C' $true
TextAt '单关地图预览 · 设计示意，非实机画面' 93 98 23 '#596D61'
TextAt '7 × 7  /  三人小队  /  无增援' 1055 63 26 '#315C4D' $true
Box 79 159 802 802 '#D6DDD0'
for($row=0;$row -lt 7;$row++) {
    for($col=0;$col -lt 7;$col++) {
        $fill='#DCE6CE'
        if(($row+$col)%2 -eq 0){$fill='#E5ECD9'}
        if($col -ge 2 -and $col -le 4){$fill='#E8E7D8'; if(($row+$col)%2 -eq 0){$fill='#EEEDE1'}}
        Box ($left+$col*$cell) ($top+$row*$cell) $cell $cell $fill
    }
}
# Artillery warning: the three empty, walkable cells B6, C6, D6.
$warnY=$top+5*$cell
Box ($left+$cell) $warnY (3*$cell) $cell '#F8DCD3'
for($k=0;$k -lt 3;$k++) {
    $wx=$left+(1+$k)*$cell
    for($h=10;$h -lt 100;$h+=20){Line ($wx+$h) ($warnY+8) ($wx+$h-7) ($warnY+30) '#DCA596' 2}
    CenterText '炮击 2' ($wx+55) ($warnY+78) 18 '#B75242' $true
}
# Fine grid.
for($i=0;$i -le 7;$i++) {
    Line ($left+$i*$cell) $top ($left+$i*$cell) ($top+7*$cell) '#B5C3AD' 1
    Line $left ($top+$i*$cell) ($left+7*$cell) ($top+$i*$cell) '#B5C3AD' 1
}
$wp=Pen '#C65C4B' 3; $wp.DashStyle=[System.Drawing.Drawing2D.DashStyle]::Dash
$gfx.DrawRectangle($wp,[single]($left+$cell+3),[single]($warnY+3),[single](3*$cell-6),[single]($cell-6));$wp.Dispose()
for($i=0;$i -lt 7;$i++) {
    CenterText ([char](65+$i)) ($left+$i*$cell+55) 135 23 '#687D6C' $true
    CenterText ($i+1) 56 ($top+$i*$cell+38) 23 '#687D6C' $true
}
# Walking direction hints, under the unit symbols.
foreach($route in @(@('C6','C5'),@('E6','E5','E4'))) {
    for($j=0;$j -lt $route.Count-1;$j++) {
        $a=CellCenter $route[$j];$b=CellCenter $route[$j+1]
        $rp=Pen '#64A698' 5;$rp.DashStyle=[System.Drawing.Drawing2D.DashStyle]::Dash
        $gfx.DrawLine($rp,[single]$a[0],[single]($a[1]-2),[single]$b[0],[single]($b[1]+10));$rp.Dispose()
    }
    $last=CellCenter $route[-1]
    Line ($last[0]-10) ($last[1]+20) $last[0] ($last[1]+7) '#3F8577' 4
    Line ($last[0]+10) ($last[1]+20) $last[0] ($last[1]+7) '#3F8577' 4
}
# Exactly five blocked cells.
foreach($wall in @('B3','B4','D5','F3','F4')) {
    $s=CellCenter $wall;$x=$s[0];$y=$s[1]
    Box ($x-44) ($y-36) 88 65 '#687B73'
    Box ($x-42) ($y-42) 84 52 '#9BA99D'
    Line ($x-42) ($y-16) ($x+42) ($y-16) '#6B7E72' 2
    Line $x ($y-42) $x ($y-16) '#6B7E72' 2
    Line ($x-18) ($y-16) ($x-18) ($y+10) '#6B7E72' 2
    CenterText $(if($wall -eq 'D5'){'断墙'}else{'石墙'}) $x ($y+30) 19 '#586D62' $true
}
# Protection link and objective.
$guard=CellCenter 'D3';$gun=CellCenter 'D2'
Line $gun[0] ($gun[1]+30) $guard[0] ($guard[1]-44) '#C5A458' 6
Circle $gun[0] ($gun[1]-8) 39 '#E3C77B'
Circle $gun[0] ($gun[1]-8) 33 '#8B6533'
Box ($gun[0]-11) ($gun[1]-8) 22 34 '#514637'
Box ($gun[0]-18) ($gun[1]+11) 36 13 '#514637'
CenterText '炮' $gun[0] ($gun[1]-34) 27 '#FFF6D7' $true
Circle ($gun[0]+31) ($gun[1]-38) 17 '#FFF6D7'
CenterText '10' ($gun[0]+31) ($gun[1]-50) 17 '#865B2F' $true
CenterText '炮座 · 目标' $gun[0] ($gun[1]+34) 18 '#8B6533' $true
Token 'D3' '卫' '护卫' '5' '#AF5D4A'
Token 'D4' '弩' '弩手' '3' '#AF5D4A'
Token 'C4' '巡' '巡兵' '4' '#AF5D4A'
Token 'C7' '射' '射手' '6' '#377C96'
Token 'D7' '先' '先锋' '9' '#377C96'
Token 'E7' '术' '术师' '6' '#377C96'
# Sidebar: concise, independently readable rules.
Box 930 175 580 150 '#E2EADD'
TextAt '胜利目标' 962 194 22 '#52755A' $true
TextAt '摧毁炮座，即刻获胜' 962 231 34 '#234A3C' $true
TextAt '不必清场 · 不等波次 · 没有后续成长' 962 283 21 '#5E7466'
TextAt '看懂地图' 952 352 27 '#234A3C' $true
Circle 972 415 12 '#377C96';TextAt '蓝色：我方三名队员' 1000 399 24
Circle 972 461 12 '#AF5D4A';TextAt '红色：巡兵 / 弩手 / 护卫' 1000 445 24
Circle 972 507 12 '#C5A458';TextAt '金色：炮座，10HP' 1000 491 24
Box 960 543 24 24 '#7D8D81';TextAt '灰石：挡路、挡视线' 1000 537 24
Box 960 587 24 24 '#F0B8A7';TextAt '红框：下一次炮击，伤害2' 1000 581 24
TextAt '炮击敌我皆伤；离开红框即可避开。' 952 630 21 '#9E594C'
TextAt '护卫邻接炮座时，炮座每次减伤2。' 952 667 21 '#7E683E'
Box 945 725 275 124 '#DFEBDC'
TextAt '治疗' 964 739 29 '#427858' $true
TextAt '1 AP  /  恢复3生命' 964 782 23 '#427858'
TextAt '受伤队员直接受益' 964 818 18 '#5A765C'
Box 1235 725 275 124 '#DCE8ED'
TextAt '护盾' 1254 739 29 '#3D7188' $true
TextAt '1 AP  /  获得3盾' 1254 782 23 '#3D7188'
TextAt '至下次己方回合，不叠加' 1254 818 17 '#537587'
TextAt '两种牌都直接拖给受益队员，无需奶妈。' 950 879 23 '#365D4C' $true
TextAt '治疗 / 护盾各2张，候选牌组共18张。' 950 917 21 '#607566'
Box 95 984 1415 2 '#C9D3C1'
TextAt '左侧近战开路  /  右侧范围压制  /  移开护卫后集中攻击' 95 1004 25 '#315C4D' $true
TextAt '目标：首次体验5–10分钟，可更快结束。数值与关卡待审核，尚未实测。' 95 1055 22 '#6B786C'
$outputPath=Join-Path $PSScriptRoot 'qinglan-layout-v1.png'
$canvas.Save($outputPath,[System.Drawing.Imaging.ImageFormat]::Png)
$gfx.Dispose();$canvas.Dispose()
Write-Output $outputPath
