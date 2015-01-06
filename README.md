##What is Inertia?

<img src="https://github.com/nettlep/Inertia/blob/master/preview.png">

Inertia is a MAME front-end with an animated 3D interface. 

The interface is a focused, clean & elegant design with a minimal, but functional interface.

There is some customization available (see below) but probably not what you're used to with other universal front-end solutions.

Inertia was written for the Windows platform, and only supports MAME emulation.

###State of the project

I have not worked on Inertia for a few years and I have no plans for further development.

However, my hope is that other developers will fork the project and continue its development. If you're a developer, you'll want to check out the *Source* folder.

###Supported MAME versions

My setup uses an old version of MAME (0.133u). I've not tested this with any recent versions of MAME, so here are the dependencies between Inertia and MAME. If something doesn't work when launching MAME, it will probably be one of these issues:

* Inertia will automatically set the MAME rotation using the MAME command line parameters ("-rotate -ror" or "-rotate -rol").
* When scanning for ROMs to include in the interface, Inertia will cross-check that a ROM is supported in the *mame.xml* file that comes with your copy of MAME. If the *mame.xml* for your version of MAME uses a different format, this may be broken (and possibly cause a crash.)

##Getting Started using Inertia

To use Inertia, simply grab Inertia's *Runtime* folder and drop your MAME installation inside that folder. If done properly, you should fine your *mame.exe* file located in the same folder as the *Inertia.exe* file. When doing this, it should ask to overwrite a few files/folders - say YES. The supplied files/folders are just for sample purposes.

After doing this, ensure your MAME executable filename is *mame.exe* (rename it if necessary) and also ensure your *roms* and *snaps* folders are not empty.

Finally, launch *Inertia.exe* (it will launch in a window by default - see below for configuration information.)

##Configuration

Inertia's configuration is completely contained in the *InertiaConfig.xml* file. If you're familiar with XML, then it should be pretty self-explanatory. If not, seek help from an XML-aware friend.

Here are some settings that may need some explanation:

* Resolution (width/height) - if set to 0, the default is 640x480.
* ScreenRotationDegrees - in 90-degree increments
* MonitorAspect - I use 1.6 for my vertical 19" CRT.
* CameraPosition & CameraTarget - these define where the camera is located (in 3-space) and what point it is looking at. Try making moderate changes to the target to see how this affects what you see.
* PanelFolders - this defines which folders are scanned to provide panels. I believe the limit is 5. The order determines their order through rotation. You can use them for anything you want - you don't have to stick with my choices. Any text file will be rendered as as scrolling text list, while images will be displayed (centered and aspect-sized) to a panel.
* MAMEParms - these parameters are always sent to the MAME executable when launched.
* EnableVideoWriteOnCoin - This will write an AVI file next to each image in the snap folder to capture video of the gameplay. Should probably leave this off.

##Customizations

The following customizations and tweaks are available that may make your life a little easier.

* You can have different copies of the *InertiaConfig.xml* file, based on the machine name. In my case, my MAME cabinet's machine name is set to *mame-cabinet*, so you'll find *mame-cabinet.InertiaConfig.xml* in the directory. This is only loaded on machines with the name *mame-cabinet*. If a machine-specific InertiaConfig.xml is not found, then it loads the non-machine specific version.
* *motions.xml* contains all the information for how panels animate. Feel free to play with this, but you'll have to do your own investigation on how the file is formatted (hint, check the source if you're a developer!)






