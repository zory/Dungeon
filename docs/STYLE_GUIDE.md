# Style Guide

## Code Style

- Explicit types everywhere unless the line becomes unwieldy
- Explicit, understandable variable names
- Do not remove existing comments; add comments where logic is non-obvious
- Always use braces, even for single-line if/for bodies
- Prefer longer lines over unnecessary splits (ultrawide monitor workflow)
- Public members: `PascalCase`
- Private members: `_lowerCamelCaseWithLeadingUnderscore`
- Constants: `SCREAMING_SNAKE_CASE`

## Art Style

2D TopDown with slight angle upwards. Similar how Stardew Valley looks like.
2D image style is simplistic, hand painted with black thin outlines. Flat colors. Can be animated both with tweens or with sprite sheets or with bone animations.
Characters are a bit more complex, but board game figure style with platform under their legs. Animated with tweens for movements and simple bone based animations for idling, fighting and so on.
Custom lightning, shadows and black and white colors matters a lot. And vivid colors where colors exists. Darkness is pitch black without colors but white outlines in black, while in light vivid colors with black outlines. Lightning is not fading it is either pitch black or fully lit.

## UI Style

Canvas style UI which separate panels for separate features. Each panel can be dragged, closed, opened in settings. This is true until game will be playable, this will be work in progress UI, feature based UI.

## Audio Style

NONE YET
