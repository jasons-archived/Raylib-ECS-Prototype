<h1 align="center">
    <a href="#"><img align="center" src="meta/logos/[!!]-logos_red.png" height="96"> NotNot Engine</a>
    <br />
</h1>
<div align="center">


![.NET 6.0](https://img.shields.io/badge/.NET-net6.0-%23512bd4)
[![Join our Discord](https://img.shields.io/badge/chat%20on-discord-7289DA)](https://discord.gg/ZyCNM7wap8)

</div>


# 🚧🚨🚧 UNDER CONSTRUCTION 🚧🚨🚧
[!!] The NotNot Engine is currently under heavy development and is not yet ready for use!


- If you want to read the raw, messy dev/WIP notes, [see here](./meta/notes.md).
- Source code is currently optimized for "getting it working".  Not for readability nor usability.
  - A cleanup+doc+usability pass will be made prior to "go live".  


Basic Execution+ECS infrastructure complete.  Now setting up OSS infrastructure and proving utility (integrating engine features).  


# Table of Contents
- [🚧🚨🚧 UNDER CONSTRUCTION 🚧🚨🚧](#-under-construction-)
- [Table of Contents](#table-of-contents)
- [NotNot?](#notnot)
- [A grand, noble experiment!](#a-grand-noble-experiment)
  - [👶 Help this baby grow! 👶](#-help-this-baby-grow-)
- [Features](#features)
  - [Complete](#complete)
  - [In progress](#in-progress)
  - [Up Next?](#up-next)
- [Licensing](#licensing)
- [Contributing](#contributing)
- [Funding](#funding)

# NotNot?
[!!] It's not not an engine. 🙃

This is the beignnings of an ECS based engine.  It is still in it's early infancy, not having any feature implemented other than a  **multithreading execution engine** and an **ecs Framework**.   

# A grand, noble experiment! 
The goal of the NotNot project is to create a modern, open-source C# engine in a sustainable fashion.  As part of that goal certain consessions must be made:

- Reduced feature set to critical path:
  - No Editor / Bells / Whistles.
  - Use off-the-shelf solutions where possible
  - Target use by skilled C# developers
  - Target Desktop runtime only (dotnet6)
- Funding/Revenue paid out to contributors
- OSS Licensed under the [AGPL](LICENSE.md), with an inexpensive [Private Commercial License](./meta/LicenseOptions.md) as an alternative.

## 👶 Help this baby grow! 👶
[Donate](https://github.com/sponsors/jasonswearingen?frequency=recurring) / [Contribute](CONTRIBUTING.md) / [Chat](https://discord.gg/ZyCNM7wap8) / [Spread the word](https://www.reddit.com/)




# Features

## Complete

- [X] ECS Framework
- [X] SimPipeline (multithreaded execution)
## In progress
- [ ] Physics system via [BepuPhysics](https://github.com/bepu/bepuphysics2)
- [ ] OSS Project organization


## Up Next?
- [ ] Rendering
- [ ] SceneGraph
- [ ] Github polish (wiki, roadmap, sponsors, etc)
- [ ] SourceCode Polish (conform to api guidelines: make it easy for you to use!)

# Licensing
By *default*, this repository is licensed to you under the **AGPL-3.0**.  See [LICENSE.md](LICENSE.md)

A very low cost Private Commercial License will also be made available to those who can not accept the AGPL terms.  Please see the [LicenseOptions.md](./meta/LicenseOptions.md) for more details.

# Contributing

Contributions are welcome!  See [CONTRIBUTING.md](./meta/CONTRIBUTING.md)

# Funding
NotNot is a grand experiment in open source: Is there room for another open source game engine?  Can it be made sustainable?  Your financial support will help make this a reality.  Sponsor NotNot through [Jason (Novaleaf)](https://github.com/sponsors/jasonswearingen?frequency=recurring).

