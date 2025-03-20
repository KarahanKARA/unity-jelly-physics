# Unity Jelly Physics

A lightweight implementation of jelly-like physics for 3D objects in Unity using vertex manipulation and spring physics.

![Jelly Physics Demo](screenshots/record.mp4)

## Overview

This project demonstrates a custom jelly/soft-body effect for 3D objects in Unity. The implementation uses vertex manipulation and spring physics to achieve realistic deformation when objects move or are interacted with.

## Features

- Realistic jelly-like deformations based on movement and interaction
- Customizable physical properties (wobble strength, stiffness, damping)
- Wave-like propagation of deformation through the mesh
- Click interaction to poke and deform objects
- Accumulative impact effect for repeated interactions
- Movement controls with configurable boundaries

## Implementation Details

The jelly effect is achieved through:
- Per-vertex spring physics simulation
- Local space velocity calculation for movement-based deformation
- Wave propagation for natural-looking ripples
- Procedural randomization using Perlin noise for organic movement
- Vertex position constraints to prevent mesh tearing

## Getting Started

1. Clone this repository
2. Open with Unity 2022.3.48f or newer
3. Explore the demo scene to see the jelly effect in action
4. Apply the JellyCube component to any mesh to create a jelly-like behavior

## Customization

The JellyCube component provides several adjustable parameters to fine-tune the jelly physics:

- **Jelly Effect Settings**: Control the strength, stiffness, and damping of the effect
- **Movement Randomization**: Add subtle variation to the movement
- **Wave Effect**: Configure how deformation propagates through the mesh
- **Velocity Limits**: Set boundaries to prevent extreme deformations

## Requirements

- Unity 2022.3.48f or newer
- Universal Render Pipeline (URP)

## License

This project is open source and available under the MIT License.
