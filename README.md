# APF-O

Unity-compatible C# scripts for redirected walking using APF-O (Artificial Potential Fields with Steer-to-Orbit).

This repository accompanies “Redirected Walking for Multi-User Extended Reality Experiences with Confined Physical Spaces,” presented at QoMEX 2025 in Madrid.

Winner of Best Student Paper at QoMEX 2025.

Note: The paper/preprint link will be added here when available.

## Overview

APF-O implements redirected walking for multi-user XR in confined physical spaces. This repository intentionally contains only the Unity C# script implementing the algorithm. No 3D assets are included.

## Requirements

- Unity 6
- Target device: Meta Quest 3 (tested on device)
- XR stack: OpenXR/XR Interaction Toolkit or equivalent

## Installation

- Copy the RDW script into your Unity project (for example, under `Assets/APF-O/`).

## Usage

1. Add your XR rig to the scene (e.g., Meta Quest Building Block or XR Origin).
2. Attach the RDW script to the rig's `Camera Offset` object (or your equivalent rig offset transform).
3. Configure the RDW parameters in the Inspector (e.g., safety radius, gains).
4. Build and deploy to Meta Quest 3.

## Citation

If this work is useful to you, please cite:

```bibtex
@inproceedings{Fite2509:Redirected,
	title        = {Redirected Walking for {Multi-User} eXtended Reality Experiences with Confined Physical Spaces},
	author       = {Gijs Fiten and Jit Chatterjee and Kobe Vanhaeren and Mattis Martens and Maria {Torres Vega}},
	year         = 2025,
	month        = sep,
	booktitle    = {2025 17th International Conference on Quality of Multimedia Experience (QoMEX) (QoMEX 2025)},
	address      = {Madrid, Spain},
	pages        = {6.52},
	days         = 29
}
```

## License

MIT
