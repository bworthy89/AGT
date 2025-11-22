import { ModRegistrar } from "cs2/modding";
import { AdvancedGridToolOptionsComponent } from "mods/AdvancedGridToolOptions";

const register: ModRegistrar = (moduleRegistry) => {
    console.log("Advanced Grid Tool UI module registrations started.");

    // Extend the mouse tool options panel like Line Tool does
    moduleRegistry.extend(
        "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx",
        'MouseToolOptions',
        AdvancedGridToolOptionsComponent(moduleRegistry)
    );

    console.log("Advanced Grid Tool UI module registrations completed.");
}

export default register;