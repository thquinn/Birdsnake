# Birdsnake
A solver for the puzzle game Snakebird

## How to Use
Uncomment the levelString for the level you want to solve, or add your own by following the key:
```
. empty
= platform
* fruit
X spike
@ portal
1234567890+- first bird
ABCDEFGHIJKL second bird
abcdefghijkl third bird
% first object
$ second object
& third object
# fourth object
/ row delimiter
```
Run the solver and wait for it to finish. Then, follow the sequence of states it outputs.

## What's Broken?
* No teleporter support.
* The game will take forever and/or run out of memory solving the game's most complex levels. Here are the levels it can currently solve:

![Solvable Levels](http://i.imgur.com/KHxAiIt.png)

## Standard Disclaimer
This code is presented as is, etc, etc, do whatever you want with it. I wrote this code in four bleary-eyed hours; it is not representative of my typical coding style of quality, so give me a break here.