# Entity Component System

## General Overview

watch the Brian Will intro to Unity ECS.  The public api is very similar.
- https://www.youtube.com/watch?v=OqzUr-Rg6w4


Some differences:
- You can use Classes or Structs.  
   - Unity forces you to use only structs.
   - We still strongly suggest using struct based components, but for archetypes that will hold few entities (such as Player archetype), Class based components are fine.
   - can be of any size

