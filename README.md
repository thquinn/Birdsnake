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
* Not much, anymore! Even with my meager 8GB of RAM, I was able to tweak it to solve everything except Star-6:

![Solvable Levels](https://i.imgur.com/U3V2rZo.png)

## Standard Disclaimer
This code is presented as is, etc, etc, do whatever you want with it. I wrote this code in six bleary-eyed hours; it is not representative of my typical coding style of quality, so give me a break here.