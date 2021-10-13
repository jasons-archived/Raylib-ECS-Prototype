<h1 align="center">
    <a href="#"><img align="center" src="meta/logos/[!!]-logos_red.png" height="96"> NotNot ECS</a>
    <br />
</h1>
<div align="center">


![Status PRE-ALPHA](https://img.shields.io/badge/status-PRE--ALPHA-red)
![Commit Activity](https://img.shields.io/github/commit-activity/m/NotNotTech/NotNot)
![.NET 6.0](https://img.shields.io/badge/.NET-net6.0-%23512bd4)
[![Join our Discord](https://img.shields.io/badge/chat%20on-discord-7289DA)](https://discord.gg/ZyCNM7wap8)

</div>


# 🚧🚨🚧 UNDER CONSTRUCTION 🚧🚨🚧
[!!] The NotNot ECS is currently **PRE-ALPHA**.  Under heavy development and is not yet ready for use!

- If you want to read the raw, messy dev/WIP notes, [see here](./meta/notes.md).
- Source code is currently optimized for "getting it working".  Not for readability nor usability.
  - A cleanup+doc+usability pass will be made prior to "go live".  
## When will this be usable? 
While the individual components of NotNot have been tested, there has not been a holistic, end-to-end validation.  
To meet this "practical" validation, the BepuPhysics engine will be used as a physics module, and once that usage is validated a general "Alpha" release of NotNot will occur.


# Table of Contents
- [🚧🚨🚧 UNDER CONSTRUCTION 🚧🚨🚧](#-under-construction-)
  - [When will this be usable?](#when-will-this-be-usable)
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
[!!] It's not not an ECS Framework. 🙃


This is the beginnings of an ECS Framework.  Capital "**F**" in **F**ramework.  You will need to design your code workflows around this.  It's not really suitable to being added to existing non-ECS projects.

# A grand, noble experiment! 
The goal of the NotNot project is to create a modern, open-source C# game tech in a sustainable fashion.  As part of that goal certain consessions must be made:

- Reduced feature set to critical path:
  - No Editor / Bells / Whistles.
  - Use off-the-shelf solutions where possible
  - Target use by skilled C# developers
  - Target DotNet6 runtime only
- Funding/Revenue paid out to contributors
- OSS Licensed under the [AGPL](LICENSE.md), with an inexpensive [Private Commercial License](./meta/LicenseOptions.md) as an alternative.

## 👶 Help this baby grow! 👶
[Donate](https://github.com/sponsors/jasonswearingen?frequency=recurring) / [Contribute](CONTRIBUTING.md) / [Chat](https://discord.gg/ZyCNM7wap8) / [Spread the word](https://www.reddit.com/)




# Features

## Complete

- [X] ECS Framework
- [X] SimPipeline (multithreaded execution)
- [X] OSS Project organization
## In progress
- [ ] Validate End-to-End by implementing a Physics Module via [BepuPhysics](https://github.com/bepu/bepuphysics2)


## Up Next?
- [ ] SourceCode Polish (conform to api guidelines: make it easy for you to use!)
- [ ] SceneGraph
- [ ] Github polish (wiki, roadmap, sponsors, etc)
- [ ] Rendering?

# Licensing
By *default*, this repository is licensed to you under the **AGPL-3.0**.  See [LICENSE.md](LICENSE.md)

A very low cost Private Commercial License will also be made available to those who can not accept the AGPL terms.  Please see the [LicenseOptions.md](./meta/LicenseOptions.md) for more details.

# Contributing

Contributions are welcome!  See [CONTRIBUTING.md](./meta/CONTRIBUTING.md)
- [Chat on Discord](https://discord.gg/ZyCNM7wap8)
- Raise an Issue

# Funding
NotNot is a grand experiment in open source: Is there room for another open source ECS Framework?  Can it be made sustainable?  Your financial support will help make this a reality.  Sponsor NotNot through [Jason (Novaleaf)](https://github.com/sponsors/jasonswearingen?frequency=recurring).

