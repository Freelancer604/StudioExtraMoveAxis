$ErrorActionPreference = 'Stop'
$source = Get-Content (Join-Path $PSScriptRoot '../Common_StudioExtraMoveAxis/StudioExtraMoveAxisPlugin.cs') -Raw
# Compile the real HS1 orb handler against lightweight engine-boundary doubles.
# Rotation writes are counted; Unity's quaternion math itself is outside this test.
$start = $source.IndexOf('    internal class CenterOrbDragHandler')
$end = $source.IndexOf('#endif', $start)
$handler = $source.Substring($start, $end - $start)
$stubs = @'
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Studio;
namespace UnityEngine {
 public class Object { public static T FindObjectOfType<T>() where T:new() { return new T(); } }
 public class MonoBehaviour {}
 public struct Vector2 { public float x,y; public Vector2(float a,float b){x=a;y=b;} public float magnitude {get{return (float)Math.Sqrt(x*x+y*y);}} public static Vector2 operator -(Vector2 a,Vector2 b){return new Vector2(a.x-b.x,a.y-b.y);} }
 public struct Vector3 { public float x; }
 public struct Quaternion { public static Quaternion identity {get{return new Quaternion();}} public Vector3 eulerAngles {get{return new Vector3();}} public static Quaternion Euler(Vector3 v){return new Quaternion();} public static Quaternion Inverse(Quaternion q){return q;} public static Quaternion AngleAxis(float a,Vector3 v){return new Quaternion();} public static Quaternion operator *(Quaternion a,Quaternion b){return a;} }
 public class Transform { public Transform parent; public Quaternion rotation; public Vector3 right,up; }
 public class Camera { public static Camera main=new Camera(); public Transform transform=new Transform(); }
}
namespace UnityEngine.EventSystems {
 public class PointerEventData { public enum InputButton {Left,Right,Middle} public InputButton button; public Vector2 position; }
 public interface IBeginDragHandler {void OnBeginDrag(PointerEventData e);}
 public interface IDragHandler {void OnDrag(PointerEventData e);}
 public interface IEndDragHandler {void OnEndDrag(PointerEventData e);}
}
namespace Studio {
 public class ChangeAmount {public int Writes; private Vector3 value; public Vector3 rot {get{return value;} set{this.value=value;Writes++;}}}
 public class GuideObject {public bool isActive=true;public Transform transformTarget=new Transform();public ChangeAmount changeAmount=new ChangeAmount();}
 public class CameraControl {public bool isCursorLock;}
}
public class GameCursor {public static GameCursor Instance=new GameCursor();public bool enabled;public void SetCursorLock(bool v){} }
public class StudioExtraMoveAxisPlugin {
 public static bool _extraGizmoDragActive, _extraGizmoShiftRollMode;
 public static HashSet<GuideObject> Selection=new HashSet<GuideObject>();
 public static HashSet<GuideObject> GetSelectedObjects(){return Selection;}
 public static void BeginRotationUndo(int button){RotationUndoActive=true;}
 public static bool RotationUndoActive=true;
 public static void TrackRotationUndo(GuideObject guide){}
 public static Vector3 ReadRotation(GuideObject guide){return guide.changeAmount.rot;}
 public static void WriteRotation(GuideObject guide,Vector3 value){guide.changeAmount.rot=value;}
 public static void EndRotationUndo(){RotationUndoActive=false;}
}
'@
$tests = @'
public static class Regression {
 static PointerEventData At(float x){return new PointerEventData{position=new Vector2(x,0)};}
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 public static void Run(){
  var oldBone=new GuideObject(); var nextBone=new GuideObject(); var orb=new CenterOrbDragHandler();
  StudioExtraMoveAxisPlugin.Selection.Add(oldBone);
  orb.OnBeginDrag(At(0)); orb.OnDrag(At(20));
  Require(oldBone.changeAmount.Writes==1,"initial drag did not rotate original bone");
  StudioExtraMoveAxisPlugin.Selection.Clear(); StudioExtraMoveAxisPlugin.Selection.Add(nextBone);
  orb.OnDrag(At(30));
  Require(oldBone.changeAmount.Writes==1,"scroll keeps writing to old joint");
  Require(nextBone.changeAmount.Writes==0,"selection change must re-anchor without a jump");
  orb.OnDrag(At(50));
  Require(nextBone.changeAmount.Writes==1,"continued drag did not rotate new joint");
  Require(oldBone.changeAmount.Writes==1,"continued drag rotated old joint too");
  StudioExtraMoveAxisPlugin._extraGizmoShiftRollMode=true; orb.OnDrag(At(60));
  Require(nextBone.changeAmount.Writes==1,"trackball writes during shift roll");
  StudioExtraMoveAxisPlugin._extraGizmoShiftRollMode=false; orb.OnDrag(At(70)); orb.OnDrag(At(90));
  Require(nextBone.changeAmount.Writes==2,"trackball did not resume after shift roll");
  StudioExtraMoveAxisPlugin.Selection.Clear(); orb.OnDrag(At(100));
  Require(nextBone.changeAmount.Writes==2,"deselection keeps rotating last bone");
  orb.OnEndDrag(At(100));
  Require(!StudioExtraMoveAxisPlugin._extraGizmoDragActive,"drag flag not released");
 }
}
'@
Add-Type -TypeDefinition ($stubs + $handler + $tests)
$failures = @()
try { [Regression]::Run(); Write-Output 'PASS: held drag retarget, no jump, shift-roll transition, deselection and release' }
catch { $failures += $_.Exception.Message }
# Evaluate the actual conditional bone list for HS1.
$listStart = $source.IndexOf('private static readonly string[] BoneCycleOrder', $source.IndexOf('#else', $source.IndexOf('#if KK || KKS')))
$listEnd = $source.IndexOf('};', $listStart)
$listText = $source.Substring($listStart, $listEnd - $listStart)
$listText = [regex]::Replace($listText, '(?s)#if HS1\s*(.*?)#else\s*.*?#endif', '$1')
$order = @([regex]::Matches($listText, '"([^"]+)"') | ForEach-Object { $_.Groups[1].Value.ToLowerInvariant() })
foreach ($side in @('r','l')) {
 $foot = [array]::IndexOf($order, "cf_j_foot01_$side")
 $toe = [array]::IndexOf($order, "cf_j_toes01_$side")
 $legacy = [array]::IndexOf($order, "ggmod_j_toes01_$side")
 if ($toe -lt 0 -or $legacy -lt 0 -or $toe -ge $foot -or $legacy -ge $foot) { $failures += "Missing or misplaced toes on side $side" }
}
if ($failures.Count) { throw ($failures -join '; ') }
Write-Output 'PASS: both standard and legacy toes follow feet when scrolling toward fingers'
