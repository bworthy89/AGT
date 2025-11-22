import { bindValue, trigger, useValue } from "cs2/api";
import { tool } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import { ModuleRegistry } from "cs2/modding";
import { Tooltip } from "cs2/ui";
import React, { Component, ReactNode } from "react";

// Value Bindings - Two-way sync with C# backend
export const isActive$ = bindValue<boolean>('AdvancedGridTool', 'IsActive');
export const showOptions$ = bindValue<boolean>('AdvancedGridTool', 'ShowOptions');
export const currentMode$ = bindValue<number>('AdvancedGridTool', 'CurrentMode');
export const spacing$ = bindValue<number>('AdvancedGridTool', 'Spacing');
export const gridWidth$ = bindValue<number>('AdvancedGridTool', 'GridWidth');
export const gridHeight$ = bindValue<number>('AdvancedGridTool', 'GridHeight');
export const showStandardOptions$ = bindValue<boolean>('AdvancedGridTool', 'ShowStandardOptions');
export const showOrganicOptions$ = bindValue<boolean>('AdvancedGridTool', 'ShowOrganicOptions');
export const showSuburbanOptions$ = bindValue<boolean>('AdvancedGridTool', 'ShowSuburbanOptions');

// Trigger Bindings - One-way commands to C# backend
export function toggleTool() { trigger("AdvancedGridTool", "ToggleTool"); }
export function setModeStandard() { trigger("AdvancedGridTool", "SetModeStandard"); }
export function setModeOrganic() { trigger("AdvancedGridTool", "SetModeOrganic"); }
export function setModeSuburban() { trigger("AdvancedGridTool", "SetModeSuburban"); }
export function setModeHexagonal() { trigger("AdvancedGridTool", "SetModeHexagonal"); }
export function setModeCurved() { trigger("AdvancedGridTool", "SetModeCurved"); }
export function setModeTerrainAdaptive() { trigger("AdvancedGridTool", "SetModeTerrainAdaptive"); }
export function increaseSpacing() { trigger("AdvancedGridTool", "IncreaseSpacing"); }
export function decreaseSpacing() { trigger("AdvancedGridTool", "DecreaseSpacing"); }
export function increaseWidth() { trigger("AdvancedGridTool", "IncreaseWidth"); }
export function decreaseWidth() { trigger("AdvancedGridTool", "DecreaseWidth"); }
export function increaseHeight() { trigger("AdvancedGridTool", "IncreaseHeight"); }
export function decreaseHeight() { trigger("AdvancedGridTool", "DecreaseHeight"); }

// Component Factory Pattern - HOC that wraps MouseToolOptions (following Line Tool pattern)
export const AdvancedGridToolOptionsComponent = (moduleRegistry: ModuleRegistry) => (Component: any) => {
    return (props: any) => {
        console.log(`[AdvancedGridTool] Component rendering at ${new Date().toISOString()}`);

        // Resolve vanilla components and styles dynamically (like Line Tool)
        const toolMouseModule = moduleRegistry.registry.get(
            "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.tsx"
        );
        const toolButtonModule = moduleRegistry.registry.get(
            "game-ui/game/components/tool-options/tool-button/tool-button.tsx"
        );
        const toolButtonTheme = moduleRegistry.registry.get(
            "game-ui/game/components/tool-options/tool-button/tool-button.module.scss"
        )?.classes;
        const mouseToolTheme = moduleRegistry.registry.get(
            "game-ui/game/components/tool-options/mouse-tool-options/mouse-tool-options.module.scss"
        )?.classes;
        const descriptionTooltipTheme = moduleRegistry.registry.get(
            "game-ui/common/tooltip/description-tooltip/description-tooltip.module.scss"
        )?.classes;
        const focusKey = moduleRegistry.registry.get("game-ui/common/focus/focus-key.ts");

        // Extract components (like Line Tool)
        const Section: any = toolMouseModule?.Section;
        const ToolButton: any = toolButtonModule?.ToolButton;
        const Tooltip: any = moduleRegistry.registry.get("game-ui/common/tooltip/tooltip.tsx")?.Tooltip;
        const FocusDisabled: any = focusKey?.FOCUS_DISABLED;

        // Get localization
        const { translate } = useLocalization();

        // Debug theme resolution
        console.log(`[AdvancedGridTool] Theme resolution:`, {
            hasDescriptionTooltipTheme: !!descriptionTooltipTheme,
            hasMouseToolTheme: !!mouseToolTheme,
            hasButtonTheme: !!toolButtonTheme
        });

        // Helper function for creating tooltips (like Line Tool)
        function TitledTooltip(titleKey: string, contentKey: string): JSX.Element {
            // Add null check for theme
            if (!descriptionTooltipTheme) {
                return <>{translate(titleKey)}: {translate(contentKey)}</>;
            }
            return (
                <>
                    <div className={descriptionTooltipTheme.title}>
                        {translate(titleKey)}
                    </div>
                    <div className={descriptionTooltipTheme.content}>
                        {translate(contentKey)}
                    </div>
                </>
            );
        }

        // Subscribe to value bindings (auto re-render on change)
        const isActive: boolean = useValue(isActive$);
        const showOptions: boolean = useValue(showOptions$);
        const currentMode: number = useValue(currentMode$);
        const spacing: number = useValue(spacing$);
        const gridWidth: number = useValue(gridWidth$);
        const gridHeight: number = useValue(gridHeight$);
        const showStandardOptions: boolean = useValue(showStandardOptions$);
        const showOrganicOptions: boolean = useValue(showOrganicOptions$);
        const showSuburbanOptions: boolean = useValue(showSuburbanOptions$);

        // Debug: Log binding values on every render
        console.log(`[AdvancedGridTool] Binding values:`, {
            isActive,
            showOptions,
            currentMode,
            spacing,
            gridWidth,
            gridHeight
        });

        // Get the original component WITHOUT props (like Line Tool!)
        let result: JSX.Element = Component();

        console.log(`[AdvancedGridTool] Component called, isActive: ${isActive}`);

        // Only show our options when the Advanced Grid Tool is active (like Line Tool)
        if (isActive) {
            // Directly push to children array (like Line Tool does)
            result.props.children?.push(
                <Section key="advancedgridtool-section" title={translate("AdvancedGridTool.TITLE", "Advanced Grid Tool")}>
                    {/* Grid Mode Selection */}
                    <div className={mouseToolTheme.row}>
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/Grid.svg"}
                            tooltip={TitledTooltip("AdvancedGridTool.StandardMode", "AdvancedGridTool.StandardMode.DESC")}
                            onSelect={setModeStandard}
                            selected={currentMode === 0}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/Organic.svg"}
                            tooltip={TitledTooltip("AdvancedGridTool.OrganicMode", "AdvancedGridTool.OrganicMode.DESC")}
                            onSelect={setModeOrganic}
                            selected={currentMode === 1}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/Suburban.svg"}
                            tooltip={TitledTooltip("AdvancedGridTool.SuburbanMode", "AdvancedGridTool.SuburbanMode.DESC")}
                            onSelect={setModeSuburban}
                            selected={currentMode === 2}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/Hexagon.svg"}
                            tooltip={TitledTooltip("AdvancedGridTool.HexagonalMode", "AdvancedGridTool.HexagonalMode.DESC")}
                            onSelect={setModeHexagonal}
                            selected={currentMode === 3}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/Curve.svg"}
                            tooltip={TitledTooltip("AdvancedGridTool.CurvedMode", "AdvancedGridTool.CurvedMode.DESC")}
                            onSelect={setModeCurved}
                            selected={currentMode === 4}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/Terrain.svg"}
                            tooltip={TitledTooltip("AdvancedGridTool.TerrainAdaptiveMode", "AdvancedGridTool.TerrainAdaptiveMode.DESC")}
                            onSelect={setModeTerrainAdaptive}
                            selected={currentMode === 5}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                    </div>

                    {/* Spacing Controls */}
                    <div className={mouseToolTheme.row}>
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/ArrowDownThickStroke.svg"}
                            tooltip={translate("AdvancedGridTool.DecreaseSpacing", "Decrease Spacing")}
                            onSelect={decreaseSpacing}
                            selected={false}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                        <Tooltip tooltip={translate("AdvancedGridTool.Spacing.DESC", "Grid spacing in meters")}>
                            <div className={mouseToolTheme.numberField}>
                                {spacing.toFixed(1)} m
                            </div>
                        </Tooltip>
                        <ToolButton
                            className={toolButtonTheme.button}
                            src={"coui://uil/Standard/ArrowUpThickStroke.svg"}
                            tooltip={translate("AdvancedGridTool.IncreaseSpacing", "Increase Spacing")}
                            onSelect={increaseSpacing}
                            selected={false}
                            multiSelect={false}
                            disabled={false}
                            focusKey={FocusDisabled}
                        />
                    </div>

                    {/* Grid Dimensions */}
                    <div className={mouseToolTheme.row}>
                        <div className={mouseToolTheme.row}>
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"coui://uil/Standard/ArrowLeftThickStroke.svg"}
                                tooltip={translate("AdvancedGridTool.DecreaseWidth", "Decrease Width")}
                                onSelect={decreaseWidth}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <Tooltip tooltip={translate("AdvancedGridTool.GridWidth.DESC", "Number of columns")}>
                                <div className={mouseToolTheme.numberField}>
                                    {gridWidth}
                                </div>
                            </Tooltip>
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"coui://uil/Standard/ArrowRightThickStroke.svg"}
                                tooltip={translate("AdvancedGridTool.IncreaseWidth", "Increase Width")}
                                onSelect={increaseWidth}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </div>
                        <div className={mouseToolTheme.row}>
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"coui://uil/Standard/ArrowDownThickStroke.svg"}
                                tooltip={translate("AdvancedGridTool.DecreaseHeight", "Decrease Height")}
                                onSelect={decreaseHeight}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                            <Tooltip tooltip={translate("AdvancedGridTool.GridHeight.DESC", "Number of rows")}>
                                <div className={mouseToolTheme.numberField}>
                                    {gridHeight}
                                </div>
                            </Tooltip>
                            <ToolButton
                                className={toolButtonTheme.button}
                                src={"coui://uil/Standard/ArrowUpThickStroke.svg"}
                                tooltip={translate("AdvancedGridTool.IncreaseHeight", "Increase Height")}
                                onSelect={increaseHeight}
                                selected={false}
                                multiSelect={false}
                                disabled={false}
                                focusKey={FocusDisabled}
                            />
                        </div>
                    </div>

                    {/* Mode-specific Options (placeholders for now) */}
                    {showStandardOptions && (
                        <div className={mouseToolTheme.row}>
                            <div className={mouseToolTheme.item}>
                                {translate("AdvancedGridTool.StandardOptions", "Standard grid options")}
                            </div>
                        </div>
                    )}
                    {showOrganicOptions && (
                        <div className={mouseToolTheme.row}>
                            <div className={mouseToolTheme.item}>
                                {translate("AdvancedGridTool.OrganicOptions", "Organic grid options")}
                            </div>
                        </div>
                    )}
                    {showSuburbanOptions && (
                        <div className={mouseToolTheme.row}>
                            <div className={mouseToolTheme.item}>
                                {translate("AdvancedGridTool.SuburbanOptions", "Suburban pattern options")}
                            </div>
                        </div>
                    )}
                </Section>
            );

            console.log(`[AdvancedGridTool] Added section to children`);
        }

        return result;
    };
};