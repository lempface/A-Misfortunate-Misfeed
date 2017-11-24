# An-Unfortunate-Misfeed
A RimWorld mod that adds the chance for guns to jam.

## Prerequisites
[HugsLib](https://github.com/UnlimitedHugs/RimworldHugsLib)

## Features
### v1.0.1
Bugfixes:
Mechanoids could not shoot at all, excluded them from the jamming system.
Grenades and Neolithic Weapons should not have a chance to jam, excluded them from the jamming system.

### v1.0

Added a chance for a gun to misfire and explode. Size of explosion based on ammunition.
Added a chance for a jammed gun to be destroyed due to the misfire.
Added a Jinxed trait that makes jamming and explosions happen at a higher rate.
Added a Proficient Armsman trait that allows for 100% chance to unjam a weapon.
Added a jamming gun sound effect.

Bugfixes:
Restored ability for turrets to fire. Turrets now skip jam checking entirely.
### v0.2
Guns may now be damaged when they jam. There are two settings in the HugsLib in-game mod settings to control this.
Damage on Jam Percentage: (default) 20%
Damage on Jam Amount: (default) 1 HP

As guns become more and more damaged, the chance to jam increases.

### v0.1
Guns jam based on the quality of the gun. These percentages can be altered using the HugsLib in-game mod settings.

If a pawn tries to fire a jammed gun, they will attempt to clear the jam first. The chance of success is based on the pawns shooting skill. There is a minimum 20% chance to clear a jam, and the chance goes up by 5% for each experience level in shooting.

## Download
[Releases](https://github.com/lempface/An-Unfortunate-Misfeed/releases)

## Road map
* Empty - Suggest something!
