https://novaleaf.itch.io/notnot-engine/devlog/307891/first-rendering
# First Rendering
![First Screenshot](./first-rendering.png)


I have been working on "NotNot Ecs Framework" for the last two months, but since it was all just code and perhaps some debug Console.Writeline() output, I didn't want to put the effort into blogging.


But today I got the rendering "working", so I thought I should start devlogging things.   I will do a devlog every time there's a marked change in the visuals to show.


The original plan was an ECS Framework to be used on top of an existing engine like Stride, but due to the extreme effort required to dev/debug/build the Stride Engine, I needed another approach.   There was no other option, so it now appears that NotNot is turning into a full blown engine itself.  NotNot is still probably another 2 months before being "usable" though.  It needs Rendering, Physics, Spatial, Input, and an example game.  As I build things out, I keep finding cases that need me to redesign the internals. like for rendering, I need to partition entities by their render mesh, which then makes me go generalize that workflow in the ecs framework itself.  Lots of 1 step forward, half step back stuff.


If you would like to know more about this project and it's features, please see the github repository.   https://github.com/NotNotTech/NotNot

I strongly feel that this is a well designed ecs framework with great potential, but I also feel that it needs to be built out into a functional example before anyone besides myself will take it seriously.


