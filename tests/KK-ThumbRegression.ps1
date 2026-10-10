$ErrorActionPreference = 'Stop'
$path = Join-Path $PSScriptRoot '../Common_StudioExtraMoveAxis/StudioExtraMoveAxisPlugin.cs'
$src = Get-Content $path -Raw
$projection = [regex]::Match($src, '(?s)else if \(IsThumbTipBone\(cn\)\)\s*\{(.*?)\n\s*\}').Groups[1].Value
$permissions = [regex]::Match($src, '(?s)else if \(IsThumbTipBone\(name\)\)\s*\{(.*?)\n\s*\}').Groups[1].Value
$right = [regex]::Match($src, 'else if \(IsThumbTipBone\(name\)\) _rightDragAxis = (.*?);').Groups[1].Value
$left = [regex]::Match($src, 'else if \(IsThumbTipBone\(name\)\) constrainedAxis = (.*?);').Groups[1].Value
$axis = [regex]::Match($src, '(?s)#if KK\s*private const string ThumbTipAxis.*?#endif').Value
foreach ($game in @('KK','HS2','HS1','KKS')) {
 $expected = if ($game -eq 'KK') {'z'} else {'y'}
 $code = @"
#define $game
using System;
namespace ThumbTest$game {
struct Quaternion {
 public float x,y,z,w;
 public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
 public static Quaternion identity {get{return new Quaternion(0,0,0,1);}}
}
static class Mathf { public static float Sqrt(float f){return (float)Math.Sqrt(f);} }
public static class Test {
$axis
 public static void Run() {
  string right=$right, left=$left;
  if(right!="$expected" || left!="$expected") throw new Exception("$game drag axis incorrect");
  bool allowX=true,allowY=true,allowZ=true;
  $permissions
  if(allowX || allowY!=("$expected"=="y") || allowZ!=("$expected"=="z")) throw new Exception("$game ring permissions incorrect");
  Quaternion qcn=new Quaternion(0.2f,0.3f,0.4f,0.5f); float num;
  $projection
  if(Math.Abs(qcn.x)>0.0001f || ("$expected"=="z" ? Math.Abs(qcn.y)>0.0001f || Math.Abs(qcn.z)<0.01f : Math.Abs(qcn.z)>0.0001f || Math.Abs(qcn.y)<0.01f)) throw new Exception("$game counter-rotation axis incorrect");
  if(Math.Abs(qcn.x*qcn.x+qcn.y*qcn.y+qcn.z*qcn.z+qcn.w*qcn.w-1)>0.0001f) throw new Exception("projection lost normalization");
 }
}
}
"@
 Add-Type -TypeDefinition $code
 $type = "ThumbTest$game.Test" -as [type]
 $type::Run()
 Write-Output "PASS: $game drag axes, ring permissions and counter-rotation projection"
}
