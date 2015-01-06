#What is Inertia?

<img src="https://github.com/nettlep/Inertia/blob/master/preview.png">

Inertia is a MAME front-end with an animated 3D interface.

The interface is a focused, clean & elegant design with a minimal, but functional interface.

There is some customization available (see below) but probably not what you're used to with other universal front-end solutions.

Inertia was written for the Windows platform, and only supports MAME emulation.

##State of the project

I have not worked on Inertia for a few years and I have no plans for further development.

However, my hope is that other developers will fork the project and continue its development. If you're a developer, you'll want to check out the *Source* folder.

###Supported MAME versions

My setup uses an old version of MAME (0.133u). I've not tested this with any recent versions of MAME, so here are the dependencies between Inertia and MAME. If something doesn't work when launching MAME, it will probably be one of these issues:

* Inertia will automatically set the MAME rotation using the MAME command line parameters ("-rotate -ror" or "-rotate -rol").
* When scanning for ROMs to include in the interface, Inertia will cross-check that a ROM is supported in the *mame.xml* file that comes with your copy of MAME. If the *mame.xml* for your version of MAME uses a different format, this may be broken (and possibly cause a crash.)

##Getting started using Inertia

To use Inertia, simply grab Inertia's *Runtime* folder and drop your MAME installation inside that folder. If done properly, you should fine your *mame.exe* file located in the same folder as the *Inertia.exe* file. When doing this, it should ask to overwrite a few files/folders - say YES. The supplied files/folders are just for sample purposes.

After doing this, ensure your MAME executable filename is *mame.exe* (rename it if necessary) and also ensure your *roms* and *snaps* folders are not empty.

Finally, launch *Inertia.exe* (it will launch in a window by default - see below for configuration information.)

###Configuration

Inertia's configuration is completely contained in the *InertiaConfig.xml* file. If you're familiar with XML, then it should be pretty self-explanatory. If not, seek help from an XML-aware friend.

My personal cabinet's config file is included under the filename *mame-cabinet.InertiaConfig.xml*.

Here are some settings that may need some explanation:

* Resolution (width/height) - if set to 0, the default is 640x480.
* ScreenRotationDegrees - in 90-degree increments
* MonitorAspect - I use 1.6 for my vertical 19" CRT.
* CameraPosition & CameraTarget - these define where the camera is located (in 3-space) and what point it is looking at. Try making moderate changes to the target to see how this affects what you see.
* PanelFolders - this defines which folders are scanned to provide panels. I believe the limit is 5. The order determines their order through rotation. You can use them for anything you want - you don't have to stick with my choices. Any text file will be rendered as as scrolling text list, while images will be displayed (centered and aspect-sized) to a panel.
* MAMEParms - these parameters are always sent to the MAME executable when launched.
* EnableVideoWriteOnCoin - This will write an AVI file next to each image in the snap folder to capture video of the gameplay. Should probably leave this off.

###Customizations

The following customizations and tweaks are available that may make your life a little easier.

* You can have different copies of the *InertiaConfig.xml* file, based on the machine name. In my case, my MAME cabinet's machine name is set to *mame-cabinet*, so you'll find *mame-cabinet.InertiaConfig.xml* in the directory. This is only loaded on machines with the name *mame-cabinet*. If a machine-specific *InertiaConfig.xml* is not found, then it loads the non-machine specific version.

* *motions.xml* contains all the information for how panels animate. Feel free to play with this. It only takes a little bit of time to get the hang of it, and then it's a lot of fun. This is described in the next section.

##Animation system

In this section, I'll try to briefly describe how the animation system works. Animations are controlled entirely by the contents of the *motions.xml* file. Just edit this file in your favorite text editor.

Be sure to keep a copy of the included *motions.xml* file (in the *Runtime* directory) available to refer to it as you read below. I've specifically left out samples from the documentation below as this file contains all the samples you'll need to follow along.

###Overview

The animation system is an *event-driven* system. That's a software term that basically means, "when a game is selected, run animation X". In our case, "game is selected" is our event, which will trigger the action (running animation X.) There are four events:

* **GameDeactivate** - The user has switched games and the current game is being deactivated. The current game should be animated off of the screen.
* **GameActivate** - The user has switched games and the new game is being activated. The new game should be animated onto the screen.
* **PanelDeactivate** - The user has switched to a new panel within the current game. The current panel should be moved out of the way to make room for the new panel.
* **PanelActivate** - The user has switched to a new panel within the current game. The new panel should be moved into the foreground for the user to view clearly.

When a game or panel is being activated, both (activate and deactivate) events are triggered at the same time and will run simultaneously. Each animation has its own timing, so even though they get triggered to start at the same time, they don't necessarily have to end at the same time.

What we've discussed thus far are *entry events* - how we trigger the start of an animation. We can also specify how animations are terminated (called *exit events*). Think about exit events like this: if we are sliding panels around and the user switches games during that animation, what do we do? Our animation system allows us to specify which animation to transition to, so that our animations can handle any interruptions or terminations.

###Animation sequences

We now know that animations start and terminate with events, but animations can flow from one to another. We call these *animation sequences*. 

If you want your panels to slide in from off-screen, then tilt up, it might be easier to separate those two animations (slide-in, tilt-up) into two separate animations, which are played one after the other. This allows you to reuse your animations (your tilt-up animation may be very handy!) and can greatly simplify things. 

Sequences can also lead to simplification just by the mere fact that you can break things up and organize them in a way that makes sense to you.

###The *AnimationDefinition* tag

Our discussion so far has centered around the way animations are triggered by events and the flow of sequences. This control-flow is managed by the **AnimationDefinition** tag in the *motions.xml* file.

Here are the specific XML attributes for the *AnimationDefinition* tag and what they do:

* **EntryEvents** - A list of events (separated by commas) that may trigger this animation. They specify an animation sequence to play to bring something onto the screen.
 * Events must be one of (GameActivate, GameDeactivate, PanelActivate, PanelDeactivate)
 * Usually, only one event is specified as you'll likely need specific animations for each event. Not all *AnimationDefinitions* need *EntryEvents* attribute (see the *Name* attribute).

* **ExitEvents** - A list of parameterized events (separated by commas) that may interrupt this animation. They specify an animation sequence to play when something is leaving the screen.
 * Events must be one of (GameActivate, GameDeactivate, PanelActivate, PanelDeactivate)
 * These are *parameterized*, which is to say that they specify an animation to transition to for a given event. For example, "GameDeactivate(Throw)" would mean that if the animation is interrupted by a *GameDeactivate* event, the system should immediately transition to the *AnimationDefinition* named "Throw".
 * Exit events may contain no parameters. This might look like "GameDeactivate()" (note the parenthesis are still present, but they are empty.) In this case, the object simply disappears from the screen and is removed. (See "Removing objects" below for more information.)

* **Panels** - A list of panel numbers (separated by commas) that this *AnimationDefinition* applies to.
 * When the system is looking for an *AnimationDefinition* to begin playing an animation, it will look at the list of panels to decide which *AnimationDefinition* to use.
 * If you want to animate different panels in different ways, you can provide multiple *AnimationDefinition*s for each set of panels. For example, one *AnimationDefinition* for panel "0" and another for panels "1,2,3,4". If both of these *AnimationDefinition*s has the same *EntryEvents* then the *AnimationDefinition* that is chosen will be the one that specifies the panel being animated.

* **Name** - The name of the *AnimationDefinition*.
 * When sequencing via the *NextAnimation* attribute, you use this name to specify the next animation (see *NextAnimation*).

* **NextAnimation** - Specifies the next animation in a sequence to play once this animation completes (see *Name* above).

###Panel lifetime -- *IMPORTANT*

Panel lifetime refers to the strategy of making sure that panels stay on the screen when they should be, and removing them them when they are no longer needed.

When an animation sequence ends, the panel will simply disappear and the system will no longer store that panel in memory or track it. Therefore, it's important that your sequences continue to run in order to keep a panel on the screen. If you don't want a panel to move, simply specify a sequence that contains no motion (but does contain a duration) it and links back to itself via the *NextAnimation* attribute. To see an example of this, look at the included *motions.xml* file and locate the animation definition named *CycleBackWait*.

**Also, it is very important** that your animations maintain strict control over when objects are removed. If a panel is animated off screen as part of deactivating a game, then that panel should eventually disappear. You can do this in one of two ways:

1. By having an animation play that moves the panel off-screen which has no *NextAnimation* (and so the sequence ends) or
2. By having an *ExitEvent* with an empty parameterized event handler.

If this is not managed properly, the system can get bogged down trying to animate and draw objects that are never in view, which will cause the system to slow down and eventually run out of memory.

###The *MotionPoints* tag

This tag controls the actual motion of a panel. It describes how it moves, rotates, resizes, how long the animation should take to run, etc.

A *MotionPoints* tag may only appear within an *AnimationDefinition* tag.

You may specify as many *MotionPoints* tags as you like within an *AnimationDefinition* tag. These will be animated in sequence, one after the other.

You'll be specifying positions in 3D space by specifying the three *coordinates* (X, Y, Z). In our world, positive X moves to the right, positive Y moves up and positive Z moves forward, away from you into the distance.

Also, for each motion, we specify four values (numbered 0 to 3). The panel will be animated through those four values to the final value (point 3). This allows each movement to follow a rudimentary 4-segmented path without having to specify multiple separate motions. This can also be used to simulate a soft curve (though, the panel will move along a straight line from each of the four points.) I added this feature to allow me to get some form of rudimentary curve in my personal animations without having to code up a bezier pathing system, which would have been overkill for my needs. This is a little clunky, so just deal with it. ;)

Here are the specific XML attributes for the *MotionPoints* tag and what they do:

* **TimeAdjust** - How the motion is interpolated.
 * Valid values are: Linear, Accelerated, Decelerated
 * If not specified, the default is *Decelerated*
* **DurationMS** - How long the animation should last, in milliseconds.
 * If not specified, the default is 1000 milliseconds (one second.)
* **Position0** - The first position within this motion
* **Position1** - The second position within this motion
* **Position2** - The third position within this motion
* **Position3** - The final position within this motion
* **Rotation0** - The first rotation within this motion
* **Rotation1** - The second rotation within this motion
* **Rotation2** - The third rotation within this motion
* **Rotation3** - The final rotation within this motion
* **Scale0** - The first scale factor within this motion
* **Scale1** - The second scale factor within this motion
* **Scale2** - The third scale factor within this motion
* **Scale3** - The final scale factor within this motion

**Notes about the attributes above:**

* If a *Position*, *Rotation* or *Scale* is not specified, then the panel is not moved, rotated or scaled (it's current position, rotation, scale factor is maintained.)

* If any *Position*, *Rotation* or *Scale* is specified, then all four (ex: *Position0*, *Position1*, *Position2*, *Position3*) must be specified.

* *Rotation* is specified in degrees.

* *Scale* is specified in terms of a *scale factor*, which is how much larger or smaller to make the object. If a value of "2.0" is entered, it is doubled in size. Likewise, a value of "0.5" will cause the panel to appear half sized. Scale factors are in 3D (X, Y and Z are specified) so that in order to double the width, you would specify (X="2.0" Y="1.0" Z="1.0"). Note how the value "1.0" is used to specify no change in size for the given dimension. Setting any coordinate of a *Scale* to "0" will cause the object to become infinitely thin in that dimension. Setting at least two values to "0" will cause it to disappear (it would become an infinitely thin line or an infinitely tiny dot.)

* A coordinate can be specified with a random number using the *Rand(...)* syntax. For example, *X="Rand(10, 20)"* would specify a random value for X in the range of 10 to 20. This is how the original *motions.xml* was able to throw panels randomly into the distance.

* A coordinate can be specified with the value "current" which means to use whatever the current value is for the coordinate. This is handy when using the multiplier shortcut (see the next bullet item.)

* Specifying all four values for a *Position*, *Rotation* or *Scale* can be cumbersome when it comes to defining movement. For example, if you want something to move in a straight line in the X direction from 3.0 to 52.0, you would need to specify the four values for X ["3.0", "19.3", "35.6", "52.0"]. To simplify this, we can simply specify the first and final value, then use multipliers for the inner two values, which will calculate the inner two values for us. These values for X would be ["3.0", "\*0.33", "\*0.66", "52.0" ]. Similarly, if we wanted to move along X from 0.0 to 100.0, the values for X would be ["0.0", "\*0.33", "\*0.66", "100.0" ]. To see this in action, be sure to look at the provided *motions.xml* file (the *CycleBack* animation definition has a good example of doing this on the Z direction, and *Throw* has a more elaborate example.)