# Testing Strategy

## Priority Areas

1. Pure Logic tests (Grid, Cell, WorldObject, Features)
2. Serialization (LevelData round-trip)
3. Procedural generation determinism
4. System behaviour (movement, interaction, combat)

## Running Tests

- **Local:** `scripts/run-unity-tests.ps1 -TestMode editmode`
- **CI:** Automatic on push/PR to main via GitHub Actions

## Coverage

- Unity tests should cover all logic parts and all features.
- Entire game flow should be testable as well. So it is possible from code and save file to restore any scenario and test any scenario. For example it is possible to check if quest X can be completed (several systems are integrated together and changed in order to to pass). Game flow tests should be done later in the project as that really depends on the content of the game and not only on features and framework, but entire system and tests should be made with that in mind.
- If feature is inside submodule, then tests should be inside that submodule as well. In case of FactorialFunShared submodule inside there is Unity project with tests folders as well, so all tests should be included in that project.