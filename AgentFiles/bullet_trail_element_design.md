# Bullet trail element design (from shader comments)

Reference for aligning visuals with intended look. C# widths/colors and shader behavior should match these.

| Element | Trail look | Core |
|--------|------------|------|
| **Arc** | Thin needle, jagged outline, discontinuous lightning filaments, flicker. White-hot tip, cyan body. | White-hot (small, 2px). |
| **Solar** | Rounded flame plume, smooth and fluid, heat bloom. White/yellow center fading to orange and ember red. Soft, blurry edges. | Element orange. |
| **Void** | Dense orb feel, dark center, purple shell, faint glowing rim. Subtle suction — trail reads slower, ominous, massive. Soft edges. | Element purple. |
| **Stasis** | Sharp crystal shard, hard faceted edges, no blur. Pale icy blue with white highlights. Precise, readable silhouette. | Element blue. |
| **Strand** | Elongated knot of thread, organic wavering edges. Neon green with yellow-green highlights. Elastic ribbons. | Element green. |
| **Kinetic** | Fallback (Luminance StandardPrimitiveShader). Simple tint. | Element kinetic (pale yellow). |

- Trail alpha: tip solid, tail faded (trail fades behind the bullet).
- Widths: Arc thinnest (needle); Void/Solar wider (orb/plume); Stasis/Strand mid.
