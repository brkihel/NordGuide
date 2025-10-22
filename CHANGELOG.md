# Changelog

## v1.0.0
- Initial stable release of **NordGuide**.
- Compass bar with N/E/S/W and tick marks.
- Minimap POIs rendered on the compass using the pin sprites.
- Correct UV/aspect handling for packed/trimmed/rotated sprites (`DataUtility.GetOuterUV` + `textureRect`).
- Dynamic size by distance; edge fade consistent with cardinals.
- Smooth distance fade to disappearance (config: `POI Disappear Distance`).
- Ping animation (pulsing) like the minimap.
- Option to hide the small HUD minimap; world map (`M`) unaffected.
- Refactor: `MinimapVisibility` extracted; `[HUD]` section consolidation; project structure cleanup.
