import { ImageResponse } from 'next/og';

export const size = { width: 32, height: 32 };
export const contentType = 'image/png';

/**
 * Dynamic favicon — single block "R" in amber on near-black background.
 * Matches the BrandMark SVG glyph (simplified for tiny sizes).
 *
 *  ▆▆▆▆░
 *  ▆░░░▆
 *  ▆▆▆▆░
 *  ▆░▆░░
 *  ▆░░▆░
 */
export default function Icon() {
  const cell = 5;
  const offsetY = 4;
  const offsetX = 7;
  const fill = '#F5A524';     // amber
  const accent = '#00D4AA';   // cyan tick
  const bg = '#0A0A09';

  const R: number[][] = [
    [1, 1, 1, 1, 0],
    [1, 0, 0, 0, 1],
    [1, 1, 1, 1, 0],
    [1, 0, 1, 0, 0],
    [1, 0, 0, 1, 0],
  ];

  return new ImageResponse(
    (
      <div
        style={{
          width: 32,
          height: 32,
          background: bg,
          position: 'relative',
          display: 'flex',
        }}
      >
        {/* Cyan tick on top */}
        <div
          style={{
            position: 'absolute',
            top: 2,
            left: offsetX,
            width: cell * 4,
            height: 2,
            background: accent,
          }}
        />
        {R.flatMap((row, ry) =>
          row.map((on, cx) =>
            on ? (
              <div
                key={`${ry}-${cx}`}
                style={{
                  position: 'absolute',
                  left: offsetX + cx * cell,
                  top: offsetY + ry * cell,
                  width: cell - 1,
                  height: cell - 1,
                  background: fill,
                }}
              />
            ) : null,
          ),
        )}
      </div>
    ),
    size,
  );
}
