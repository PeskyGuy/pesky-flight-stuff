# Pesky's Flight Framework

This mod adds a complete flight system to RimWorld. Pawns can fly using genes, apparel (like thruster boots), or bionic implants. When a pawn flies, they ignore terrain movement penalties, hover over traps, and get a floating animation with directional tilting.

The system is built as a framework. You can easily add your own wings, jetpacks, or hover-implants using just XML.

## How it works

The core of the mod is the `FlightSourceRegistry`. A pawn can have multiple things giving them flight capabilities at the same time. For example, a pawn with natural wings might equip thruster boots. The registry looks at all available flight sources and uses a priority system to decide which one takes over. 

By convention:
* Genes: Priority 100
* Apparel: Priority 200
* Implants: Priority 300

Higher priority sources suppress lower ones. If you turn on your thruster boots, your biological wings stop flapping and you use the boots' animation instead. If you lose or unequip an active flight source, the framework falls back to your next available flight source automatically. You won't just drop out of the sky if your jetpack breaks while you have functional biological wings.

## Adding a Flight Gene

To add a new flying xenotype or gene, you just need a few XML nodes.

```xml
<GeneDef>
  <defName>MyCustomWings</defName>
  <label>custom wings</label>
  <geneClass>Pesky.Gene_Flight</geneClass>
  
  <modExtensions>
    <li Class="Pesky.FlightSourceExtension">
      <priority>100</priority>
      <sourceType>Gene</sourceType>
      <!-- Optional tweaks -->
      <cruiseHeight>1.3</cruiseHeight>
      <tiltAngle>15</tiltAngle>
      <iconPath>UI/Icons/MyCustomFlightIcon</iconPath>
    </li>
  </modExtensions>

  <renderNodeProperties>
    <li>
      <workerClass>Pesky.PawnRenderNodeWorker_FlightAnimated</workerClass>
      <nodeClass>Pesky.PawnRenderNode_FlightAnimated</nodeClass>
      <texPath>Things/Pawn/MyWings/MyWings</texPath>
      <drawSize>1.1</drawSize>
      <parentTagDef>Body</parentTagDef>
      <drawData>
        <defaultData><layer>-2</layer></defaultData>
        <!-- Setup your north/east/west layers here -->
      </drawData>
    </li>
  </renderNodeProperties>
</GeneDef>
```

## Adding Flight Apparel (Jetpacks, Hover Boots)

Apparel uses `CompFlightApparel` to track fuel or charges. If you attach a `CompApparelReloadable` (the comp the vanilla jump pack uses), the flight comp will automatically drain its charges while flying. If you don't use the reloadable comp, it uses an internal fuel gauge that recharges over time.

```xml
<ThingDef ParentName="ApparelMakeableBase">
  <defName>MyHoverBoots</defName>
  
  <comps>
    <li Class="Pesky.CompProperties_FlightApparel">
      <maxFuel>100</maxFuel>
      <fuelDrainPerTick>0.05</fuelDrainPerTick>
      <fuelRechargePerTick>0.02</fuelRechargePerTick>
    </li>
    <!-- Optional: Add CompApparelReloadable here to consume actual items like Chemfuel -->
  </comps>

  <modExtensions>
    <li Class="Pesky.FlightSourceExtension">
      <priority>200</priority>
      <sourceType>Apparel</sourceType>
    </li>
  </modExtensions>

  <apparel>
    <renderNodeProperties>
      <!-- Use Pesky.PawnRenderNode_FlightAnimated just like the gene example -->
    </renderNodeProperties>
  </apparel>
</ThingDef>
```

## Adding Bionic Flight Implants

Implants work almost exactly like apparel. Add the comp to the `HediffDef` and set up the extension and render nodes.

```xml
<HediffDef>
  <defName>MyHoverSpine</defName>
  <comps>
    <li Class="Pesky.HediffCompProperties_FlightImplant">
      <maxFuel>100</maxFuel>
      <fuelDrainPerTick>0.04</fuelDrainPerTick>
      <fuelRechargePerTick>0.03</fuelRechargePerTick>
    </li>
  </comps>
  <modExtensions>
    <li Class="Pesky.FlightSourceExtension">
      <priority>300</priority>
      <sourceType>Implant</sourceType>
    </li>
  </modExtensions>
  <!-- renderNodeProperties go here -->
</HediffDef>
```

## Animation and Graphics

The mod expects a specific folder structure for animated flight graphics. If your `<texPath>` is `Things/MyWings/Wings`, the game will look for standard RimWorld directional textures there for when the pawn is idle or walking normally.

For the flying animation, create a subfolder called `Animation` right next to those files. Inside, you need four specific files for the animation frames:
* `Wings_Up.png`
* `Wings_Mid1.png`
* `Wings_Down.png`
* `Wings_Mid2.png`

The framework automatically cycles through these frames while the pawn is in the air. For rendering order, North, East, and West facing flight textures render in front of the pawn. South facing textures render behind them.

### Fine-Tuning the Feel

You can change how the flight looks and feels by adding these fields to your `FlightSourceExtension`:

* `cruiseHeight`: How high the pawn floats off the ground (default 1.3).
* `riseFallSpeed`: How fast they take off and land (default 0.05).
* `tiltAngle`: How far forward they lean when moving (default 15).
* `bobAmplitude`: How much they bounce up and down while hovering (default 0.08).
* `frameTicksNormal`: How fast the wings flap during normal flight (default 10 ticks per frame).
* `frameTicksTransition`: Flap speed while taking off or landing (default 6).
* `iconPath`: Custom gizmo icon for the toggle button.

## Soft Dependencies (External Genes)

If you want to add flight to a gene from another mod without making this framework a hard requirement, you can. The framework uses reflection to look for a `flightEnabled` boolean field on the gene class. Just patch a `FlightSourceExtension` onto their `GeneDef` using standard XML patching. As long as their gene has a public `flightEnabled` field, the framework takes over and makes it work automatically.
