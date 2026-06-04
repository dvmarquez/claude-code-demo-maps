const DEFAULT_LAT = -0.1806532;
const DEFAULT_LNG = -78.4678382;

export function MapPortlet({ latitude, longitude, address }) {
  const lat = latitude ?? DEFAULT_LAT;
  const lng = longitude ?? DEFAULT_LNG;
  const src = `https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`;
  const title = address ? `Mapa de ubicación: ${address}` : 'Mapa — ubicación predeterminada';

  return (
    <div className="w-full h-96 rounded-xl overflow-hidden border border-gray-200 shadow-sm">
      <iframe
        key={`${lat},${lng}`}
        title={title}
        src={src}
        className="w-full h-full"
        loading="lazy"
        referrerPolicy="no-referrer-when-downgrade"
      />
    </div>
  );
}
