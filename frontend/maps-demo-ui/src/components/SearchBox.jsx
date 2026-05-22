import { useState, useRef, useEffect } from 'react';
import { v4 as uuidv4 } from 'uuid';
import { useAutocomplete } from '../hooks/useAutocomplete';
import { useGeocode } from '../hooks/useGeocode';

export function SearchBox({ onGeocodeSuccess }) {
  const [input, setInput] = useState('');
  const [showDropdown, setShowDropdown] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);
  const sessionTokenRef = useRef(null);

  useEffect(() => {
    setActiveIndex(-1);
  }, [input]);

  const { data: suggestions = [], isLoading: isLoadingAutocomplete } = useAutocomplete(
    input,
    sessionTokenRef.current
  );
  const { mutate: geocode, isPending: isGeocoding, error: geocodeError } = useGeocode();

  const handleChange = (e) => {
    if (!sessionTokenRef.current) sessionTokenRef.current = uuidv4();
    setInput(e.target.value);
    setShowDropdown(true);
  };

  const handleKeyDown = (e) => {
    if (!showDropdown || suggestions.length === 0) return;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setActiveIndex(prev => Math.min(prev + 1, suggestions.length - 1));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setActiveIndex(prev => Math.max(prev - 1, 0));
    } else if (e.key === 'Enter' && activeIndex >= 0) {
      e.preventDefault();
      handleSelect(suggestions[activeIndex]);
    } else if (e.key === 'Escape') {
      setShowDropdown(false);
      setActiveIndex(-1);
    }
  };

  const handleSelect = (suggestion) => {
    geocode(
      { placeId: suggestion.placeId, address: suggestion.description, sessionToken: sessionTokenRef.current },
      {
        onSuccess: (result) => {
          setInput('');
          setShowDropdown(false);
          sessionTokenRef.current = null;
          onGeocodeSuccess?.(result);
        },
      }
    );
  };

  const handleBlur = () => setTimeout(() => setShowDropdown(false), 150);

  return (
    <div className="relative w-full">
      <div className="flex items-center gap-2">
        <input
          id="address-search"
          type="text"
          role="combobox"
          aria-label="Buscar dirección"
          aria-autocomplete="list"
          aria-expanded={showDropdown && suggestions.length > 0}
          aria-controls="suggestions-listbox"
          aria-activedescendant={activeIndex >= 0 ? `suggestion-${activeIndex}` : undefined}
          value={input}
          onChange={handleChange}
          onFocus={() => input.length >= 3 && setShowDropdown(true)}
          onBlur={handleBlur}
          onKeyDown={handleKeyDown}
          placeholder="Buscar dirección..."
          disabled={isGeocoding}
          className="w-full px-4 py-3 rounded-lg border border-gray-300 shadow-sm text-base
                     focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent
                     disabled:bg-gray-100 disabled:cursor-wait"
        />
        {(isLoadingAutocomplete || isGeocoding) && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2">
            <div className="w-5 h-5 border-2 border-blue-500 border-t-transparent rounded-full animate-spin" />
          </div>
        )}
      </div>

      {geocodeError && (
        <p className="mt-1 text-sm text-red-600">
          Error al obtener coordenadas. Intente de nuevo.
        </p>
      )}

      <div aria-live="polite" aria-atomic="true" className="sr-only">
        {showDropdown && suggestions.length > 0
          ? `${suggestions.length} sugerencia${suggestions.length !== 1 ? 's' : ''} disponible${suggestions.length !== 1 ? 's' : ''}. Use las flechas para navegar.`
          : ''}
      </div>

      {showDropdown && suggestions.length > 0 && (
        <ul
          id="suggestions-listbox"
          role="listbox"
          aria-label="Sugerencias de dirección"
          className="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg overflow-hidden"
        >
          {suggestions.map((s, i) => (
            <li
              key={s.placeId}
              id={`suggestion-${i}`}
              role="option"
              aria-selected={activeIndex === i}
              onMouseDown={() => handleSelect(s)}
              className={`px-4 py-3 text-sm text-gray-800 cursor-pointer border-b border-gray-100 last:border-0
                         ${activeIndex === i ? 'bg-blue-50' : 'hover:bg-blue-50'}`}
            >
              {s.description}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
