import { useState, useRef, useEffect } from "react";
import { Info } from "lucide-react";
import StartTourButton from "@/layouts/StartTourButton";

export default function TourInfoPopup() {
  const [open, setOpen] = useState(false);
  const popupRef = useRef<HTMLDivElement>(null);

  // Close popup when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (popupRef.current && !popupRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div className="relative inline-block" ref={popupRef}>
      {/* Info Icon */}
      <button
        onClick={() => setOpen(!open)}
        className="p-2 rounded-full hover:bg-gray-200 transition"
      >
        <Info className="h-5 w-5 text-gray-700" />
      </button>

      {/* Popup */}
      {open && (
        <div
          className="absolute right-0 mt-2 w-48 bg-white shadow-lg rounded-lg p-4 border border-gray-200 animate-fadeIn z-50"
        >
          <h3 className="text-sm font-semibold mb-2">Tour Guide</h3>

          <StartTourButton />

          {/* Triangle Arrow */}
          <div className="absolute -top-2 right-3 w-3 h-3 bg-white rotate-45 border-l border-t border-gray-200"></div>
        </div>
      )}
    </div>
  );
}
