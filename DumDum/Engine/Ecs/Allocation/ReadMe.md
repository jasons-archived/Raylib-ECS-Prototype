# ECS Allocation

The allocator is an important feature because in the ECS system these things need to be created and managed for each entity created:
- EntityHandle
- Each component the entity will use
  - Metadata
  - WorldXform
  - Physics info
  - rendering info
  - etc.... (whatever arbitrary data you want)

As part of managing this data, you need to:
- Query one, or multiple entities (it's components) through the course of teh game
- create/destroy entities and their components
- Systems need to efficiently query component data

To do this, the Allocation system was created.  It is comprised of the following main parts:
- `Allocator`: one per archetype.  This manages allocation and tracking of all that archetypes components
- `Chunk<TComponent>`: An array holding components of a certain type, for some of the entities of the archetype.  Imagine an archetype with 10,000 entities.  Instead of putting all the Xform components in a single huge array, we split it into smaller arrays, called chunks.  
- `AllocHandle`: tracks and provides direct access to each component for a given Entity.  If the allocator is set to `autoPack==true` (the default) this handle is only good for the current frame.  After that it needs to be reaquired.  This is provided for flexibility but for builk component query/write operations it is much more efficient to use the `Allocator.Query()` methods

Here is how thigs are laid out logically in memory:












## Remarks

- drawings created using https://asciiflow.com/#/
- 