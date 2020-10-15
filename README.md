# Learning to Grasp Objects in Virtual Environments through Imitation Project

This is the project used in the thesis Learning to Grasp Objects in Virtual Environments through Imitation by Alexandre Filipe. 
The project consists of a Virtual Environment made in Unity (v2019.4) that contains a hand and several object for it to interact with. In this Environment, using the VMG35 Haptic Glove, a glove with sensors, several recordings of tasks were performed, having the glove controlling the virtual hand in real time. Theses recordings were later used to train an LSTM to test is quality in training virtual robots to perform virtual tasks using human demonstrations.
The tasks consists in:
- Grabbing a hammer and placing it on a shelf
- Grabbing a water botte and placing it on a base
- Grabbing a can, rotating it and placing it on a box
- Grabbing a knife and placing it on a base
- Grabbing a Mug and placing it on a base
- Grabbing a parallelepiped an placing it on a square slot

More objects can be found on the Virtual Environment, but do not include recordings as they were used for other purposes.

The recordings are found in Assets/Recordings/Demos. Due to how the LSTMs were trained the recordings are segmented in 2 files/phases: reach for the movement until the object is grabbed and manip for after the object is grabbed. The files consist of a sequence of iterations, captured at each fifth of a second. Each iteration consists of 20 values: 13 finger joint bending values, 3 positional (XYZ) and 4 rotational (a quaternion) values that define the position and rotation of the hand.
The position and rotation of the hand written on the files is the relative position to an object, on the reach phase to the object to be grabbed, and on the manip phase to the object they will latter interact with.
Videos of an example of the tasks being performed are/will be present in Assets/LSTMOuts/videos.
For more detailed information check chapter 4 of the thesis.

## Working With the Virtual Environment

The object Arms_Model contains the hand and the scripts that make it works, in particular the HandController.cs. In this script the following values and flags have these purposes:

- Recording: Flag to indicate if we want to record a demonstration (using the glove)
- Autopilot: Flag to indicate if the hand will be moving autonomously
- Online: Flag dependent of Autopilot. If true the hand will follow the values it receives on the selected port (used to make the LSTMs communicate with the VE in real time), if false will follow the values written on the file Assets/LSTMOuts/aHand.txt.
- ChosenObject: Object to be grasped
- Target: Second object it will interact with
- Force closure: Flag that, if true, writes a file in Assets/LSTMOuts/ForceClosure that contains the force vectors, to be used to check if the grasp is considered a force closure
- Hand Model: Model of the hand to be used: 0 - skeletal hand, 1 - Vizzy Hand, 2 - Icub Hand. Automatically changes the hand model
The remaining public objects refers to how the hand moves/what child objects are x finger joint, etc, and is recommended to not be changed.

The objects to be grabbed also contain the randomizer.cs script that places a object at a random place at the start of a recording or reproduction. The range and rotation of the random position can be altered in this script.
