Note:  The original repo for this is: https://git.srv.hoerberg.de/tom300z/4ksp

<center><h2>4kSP 0.1</h2></center>

While most UI elements are scalable by default, the Map icons and ther labels (the orbit apoapsis for example) are not, that makes the game almost unplayable at high resolutions. This Mod scales those things using the stock UI Scale setting.  

Comparison Image (at 4k resolution):  
![](https://i.imgur.com/rf7YPdg.jpg)  

This add on is still in early development so some things might appear offset or incorrectly scaled. It's also my first add on for KSP, so please feel free to leave feedback or <a href="https://git.srv.hoerberg.de/tom300z/4ksp/issues">report an issue</a> so i can improve it.
<center>
<h4>Downloads</h4>
<a href="https://github.com/KSP-CKAN/CKAN/releases" target="_blank">CKAN</a><br/>
<a href="https://spacedock.info/mod/2228/4kSP%20-%20Scaled%20map%20icons%20and%20text" target="_blank">Spacedock</a><br/>
<a href="" target="_blank">Gitlab</a>
<h4>FAQ</h4>
<strong>Q: </strong>How do i activate this mod?<br/>
<strong>A: </strong>4kSP scales the map elements corresponding to the stock UI scale setting. The UI scale slider can be found under <strong>Settings > General > UI-Scale</strong>.
<h4>License & Source</h4>
This add on is available under the <a href="https://git.srv.hoerberg.de/tom300z/4ksp/blob/master/LICENSE">MIT License</a> and the source code is available on <a href="https://git.srv.hoerberg.de/tom300z/4ksp">Gitlab</a>.
</center>



Originally written by @tom300z, original thread is here: https://forum.kerbalspaceprogram.com/index.php?/topic/188213-17x-4ksp-scaled-map-icons-and-text/

I needed to have an alternate size from the stock UI_SCALE, so I've taken this and expanded it by adding a window to dynamically scale the map icons and text.

https://i.imgur.com/rf7YPdg.jpg

 

The **4kSP Expanded Settings** toolbar button is available in all scenes,
including the main menu, flight, editor, tracking station, and R&D.

One persistent window contains **Map Scaling**, **R&D Scaling**, and
**Mod Window Scaling** tabs. Reopening displays the current applied values.
Map settings belong to the loaded save; load a save before editing them.
R&D geometry changes apply only while its complex is open and are restored
on exit. R&D and mod-window settings can be saved globally.

The settings menu remains available without Harmony. The mod-window scaler
retains the original implementation from master; no shared UI coordinate
plane, call-site scanner, or KER parenting experiment is included.


Availability

Source: https://github.com/linuxgurugamer/4ksp_Expanded
Download: https://spacedock.info/mod/2554/
License: MIT
Available via CKAN

Dependencies

Click Through Blocker
ToolbarController
