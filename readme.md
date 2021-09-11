


# DumDum?
A codename?

This is an *execution engine* and an *ecs* framework.  Not library.  You must build your game on top of, not next to this.

# Current status

still building first functional prototype.   



# Overview

## ECS
The ECS api is heavily inspired by Unity ECS.  very similar api surface.

## ExecManager

Manages execution of "nodes" on top of it.  

Scheduling means:
- How often a node should execute
- What should the node wait for before executing
- What resources a node needs read/write access to



The ECS system will sit on top of this, and other things can too.

Currently single threaded.  API for multithreading will stay the same



## notes / scratch stuff below...




Logical object structure is

- SomeExternalTickPump
  - ExecManager
    - World
      - Gameplay systems
    - RenderingSystem
    - 

