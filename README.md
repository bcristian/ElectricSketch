# ElectricSketch
Library and program for designing and simulating electrical circuits.

The goal of the program is to assist in the design and simulation of electrical circuits, especially those used for command and automation of machinery.
Particularly the niche between "more than I can easily figure out in my head" and "not so complex that it needs (p)spice".
The area where a relay is sufficiently specified by the coil tension and the type and number of contacts, without caring about the resistance and inductance of the coil.
The area where you want to interactively toggle switches and see the effect, without having to jump through hoops, like setting up voltage triggers.

## Circuit model
The program models the electric devices from a functional point of view. A relay is something that toggles switches when energized. A motor turns if powered. The program does not attempt to properly model the physical phenomena.

### Series connections
The program does not allow series connection of consumers. This cannot be modeled without proper physical simulation. It is also something not usually desirable in automation circuits.

### Dangerous switch error
Most switches have a parameter "Allow Incompatible Potentials". If false, the simulation will throw an error if it detects different potentials across the terminals of an open switch. The reason for this behavior is that you don't want to be able to flick a switch and cause a short-circuit. There are however circuits where the flicking of the switch removes the conflict, e.g. by simultaneously disconnecting the other branch.
Generally, simple switches are set to raise the error, while more complex ones are not.

## License
The project is licensed under GPL v3.

## Credits
The toolbar icons are from https://boxicons.com/