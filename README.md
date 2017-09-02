------------
#### testhg This great software has been modified
* The timer for automatic periodic snapshots has been removed 
* Program argument for starting application with 2nd icon: WindowsLayoutSnapshot.exe /Use2ndIcon
* Added feature: Remove snapshots manually by pressing Shift and clicking on snapshot

The periodic snapshot causes flickering virtual desktops and 
switch to the virtual desktop where the unity mode vmware applications are located.
So before detaching the laptop from the dockingstation, I run manually the snapshot.


I am using my Windows 10 laptop in three enviroments:
* At Home:       Laptop attached to dockingstation which is connected to six   FullHD monitors.
* At Office:     Laptop attached to dockingstation which is connected to three FullHD monitors.
* In the train:  Laptop-only 


ToDos/Features to Add:
* The VirtualDesktop number of each window should be stored&restored, too
  After reattaching to a dockingstation, some applications (e.g. Chrome) do not stay in their assigned virtual desktop.
  Those windows have to be manually moved back to the virtual desktop.
  See also http://stackoverflow.com/questions/31801402/api-for-windows-10-virtual-desktops
  
------------
