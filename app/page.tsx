"use client";

import { useState, useCallback } from "react";
import type { Business } from "./api/search/route";

const ALL_CATEGORIES = [
  "auto detailing",
  "mobile auto detailing",
  "paint protection film",
  "ceramic coating",
  "HVAC",
  "air conditioning repair",
  "electrician",
  "electrical contractor",
  "mold removal",
  "mold remediation",
  "siding contractor",
  "roofing contractor",
  "plumber",
  "plumbing",
  "home cleaning",
  "house cleaning",
  "pressure washing",
  "window cleaning",
  "gutter cleaning",
  "carpet cleaning",
  "junk removal",
  "landscaping",
  "pest control",
  "handyman",
  "painting contractor",
  "drywall contractor",
  "insulation contractor",
  "garage door repair",
  "locksmith",
  "pool cleaning",
  "tree service",
];

export default function Home() {
  const [city, setCity] = useState("");
  const [selectedCategories, setSelectedCategories] = useState<string[]>(ALL_CATEGORIES.slice(0, 8));
  const [noWebsiteOnly, setNoWebsiteOnly] = useState(false);
  const [businesses, setBusinesses] = useState<Business[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searched, setSearched] = useState(false);
  const [expandedCategories, setExpandedCategories] = useState(false);

  const toggleCategory = (cat: string) => {
    setSelectedCategories((prev) =>
      prev.includes(cat) ? prev.filter((c) => c !== cat) : [...prev, cat]
    );
  };

  const search = useCallback(async () => {
    if (!city.trim()) return;
    setLoading(true);
    setError(null);
    setBusinesses([]);
    setSearched(false);

    try {
      const params = new URLSearchParams({
        city: city.trim(),
        categories: selectedCategories.join(","),
        no_website: String(noWebsiteOnly),
      });
      const res = await fetch(`/api/search?${params}`);
      const data = await res.json();
      if (!res.ok) throw new Error(data.error || "Search failed");
      setBusinesses(data.businesses || []);
      setSearched(true);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unknown error");
    } finally {
      setLoading(false);
    }
  }, [city, selectedCategories, noWebsiteOnly]);

  const exportCSV = () => {
    const headers = ["Name", "Category", "Address", "Phone", "Email", "Website", "GMB URL", "Rating", "Reviews", "Has Website"];
    const rows = businesses.map((b) => [
      `"${b.name}"`,
      `"${b.category}"`,
      `"${b.address}"`,
      b.phone || "",
      b.email || "",
      b.website || "",
      b.gmb_url,
      b.rating || "",
      b.review_count || "",
      b.has_website ? "Yes" : "No",
    ]);
    const csv = [headers.join(","), ...rows.map((r) => r.join(","))].join("\n");
    const blob = new Blob([csv], { type: "text/csv" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `leads-${city.replace(/\s+/g, "-").toLowerCase()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const noWebsiteCount = businesses.filter((b) => !b.has_website).length;

  return (
    <div className="min-h-screen bg-gray-950">
      {/* Header */}
      <header className="border-b border-gray-800 bg-gray-900">
        <div className="max-w-6xl mx-auto px-4 py-5">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-lg bg-blue-600 flex items-center justify-center text-white font-bold text-sm">L</div>
            <div>
              <h1 className="text-xl font-bold text-white">Locals Finder</h1>
              <p className="text-xs text-gray-400">Service business lead generator</p>
            </div>
          </div>
        </div>
      </header>

      <main className="max-w-6xl mx-auto px-4 py-8 space-y-6">
        {/* Search Card */}
        <div className="bg-gray-900 rounded-xl border border-gray-800 p-6 space-y-5">
          {/* City Input */}
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">City / Location</label>
            <div className="flex gap-3">
              <input
                type="text"
                value={city}
                onChange={(e) => setCity(e.target.value)}
                onKeyDown={(e) => e.key === "Enter" && search()}
                placeholder="e.g. Dallas, TX or Miami"
                className="flex-1 bg-gray-800 border border-gray-700 text-white placeholder-gray-500 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
              <button
                onClick={search}
                disabled={loading || !city.trim() || selectedCategories.length === 0}
                className="bg-blue-600 hover:bg-blue-500 disabled:opacity-40 disabled:cursor-not-allowed text-white font-semibold px-6 py-2.5 rounded-lg text-sm transition-colors"
              >
                {loading ? "Searching..." : "Search"}
              </button>
            </div>
          </div>

          {/* Filters Row */}
          <div className="flex items-center gap-4 flex-wrap">
            <label className="flex items-center gap-2 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={noWebsiteOnly}
                onChange={(e) => setNoWebsiteOnly(e.target.checked)}
                className="w-4 h-4 accent-blue-500"
              />
              <span className="text-sm text-gray-300">Only show businesses <strong className="text-white">without a website</strong></span>
            </label>
          </div>

          {/* Categories */}
          <div>
            <div className="flex items-center justify-between mb-3">
              <label className="text-sm font-medium text-gray-300">
                Service Categories <span className="text-gray-500 font-normal">({selectedCategories.length} selected)</span>
              </label>
              <div className="flex gap-3 text-xs">
                <button onClick={() => setSelectedCategories(ALL_CATEGORIES)} className="text-blue-400 hover:text-blue-300">All</button>
                <button onClick={() => setSelectedCategories([])} className="text-gray-400 hover:text-gray-300">None</button>
                <button onClick={() => setExpandedCategories((v) => !v)} className="text-gray-400 hover:text-gray-300">
                  {expandedCategories ? "Collapse" : "Show all"}
                </button>
              </div>
            </div>
            <div className="flex flex-wrap gap-2">
              {(expandedCategories ? ALL_CATEGORIES : ALL_CATEGORIES.slice(0, 12)).map((cat) => (
                <button
                  key={cat}
                  onClick={() => toggleCategory(cat)}
                  className={`text-xs px-3 py-1.5 rounded-full border transition-colors ${
                    selectedCategories.includes(cat)
                      ? "bg-blue-600 border-blue-500 text-white"
                      : "bg-gray-800 border-gray-700 text-gray-400 hover:border-gray-500"
                  }`}
                >
                  {cat}
                </button>
              ))}
              {!expandedCategories && ALL_CATEGORIES.length > 12 && (
                <button
                  onClick={() => setExpandedCategories(true)}
                  className="text-xs px-3 py-1.5 rounded-full border border-dashed border-gray-600 text-gray-500 hover:text-gray-300"
                >
                  +{ALL_CATEGORIES.length - 12} more
                </button>
              )}
            </div>
          </div>
        </div>

        {/* Error */}
        {error && (
          <div className="bg-red-950 border border-red-800 text-red-300 rounded-xl p-4 text-sm">
            <strong>Error:</strong> {error}
            {error.includes("API key") && (
              <p className="mt-2 text-red-400">
                Add <code className="bg-red-900 px-1 rounded">GOOGLE_PLACES_API_KEY=your_key</code> to your{" "}
                <code className="bg-red-900 px-1 rounded">.env.local</code> file and restart the server.
              </p>
            )}
          </div>
        )}

        {/* Loading */}
        {loading && (
          <div className="space-y-3">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="bg-gray-900 rounded-xl border border-gray-800 p-5 animate-pulse">
                <div className="flex justify-between">
                  <div className="space-y-2 flex-1">
                    <div className="h-4 bg-gray-700 rounded w-1/3"></div>
                    <div className="h-3 bg-gray-800 rounded w-1/2"></div>
                    <div className="h-3 bg-gray-800 rounded w-1/4"></div>
                  </div>
                  <div className="h-6 w-20 bg-gray-700 rounded-full"></div>
                </div>
              </div>
            ))}
            <p className="text-center text-gray-500 text-sm">Searching {selectedCategories.length} categories in {city}…</p>
          </div>
        )}

        {/* Results */}
        {searched && !loading && (
          <>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-white font-semibold">
                  {businesses.length} business{businesses.length !== 1 ? "es" : ""} found in <span className="text-blue-400">{city}</span>
                </p>
                {noWebsiteOnly && (
                  <p className="text-gray-400 text-sm">{noWebsiteCount} without a website — prime outreach targets</p>
                )}
                {!noWebsiteOnly && noWebsiteCount > 0 && (
                  <p className="text-green-400 text-sm">{noWebsiteCount} without a website</p>
                )}
              </div>
              {businesses.length > 0 && (
                <button
                  onClick={exportCSV}
                  className="text-sm bg-gray-800 hover:bg-gray-700 border border-gray-700 text-gray-300 px-4 py-2 rounded-lg transition-colors"
                >
                  Export CSV
                </button>
              )}
            </div>

            {businesses.length === 0 ? (
              <div className="text-center py-16 text-gray-500">
                <p className="text-lg">No results found for "{city}"</p>
                <p className="text-sm mt-1">Try a different city or select more categories</p>
              </div>
            ) : (
              <div className="space-y-3">
                {businesses.map((b) => (
                  <BusinessCard key={b.place_id} business={b} />
                ))}
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

function BusinessCard({ business: b }: { business: Business }) {
  return (
    <div className={`bg-gray-900 rounded-xl border p-5 transition-colors ${!b.has_website ? "border-green-800 bg-green-950/10" : "border-gray-800"}`}>
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 flex-wrap">
            <h3 className="text-white font-semibold text-base truncate">{b.name}</h3>
            <span
              className={`shrink-0 text-xs px-2 py-0.5 rounded-full font-medium ${
                b.has_website
                  ? "bg-gray-700 text-gray-300"
                  : "bg-green-900 text-green-300 border border-green-700"
              }`}
            >
              {b.has_website ? "Has website" : "No website"}
            </span>
            {b.status === "CLOSED_PERMANENTLY" && (
              <span className="text-xs px-2 py-0.5 rounded-full bg-red-900 text-red-300">Permanently closed</span>
            )}
          </div>

          <p className="text-xs text-blue-400 mt-0.5 font-medium">{b.category}</p>
          <p className="text-sm text-gray-400 mt-1">{b.address}</p>

          <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1.5 text-sm">
            {b.phone && (
              <a href={`tel:${b.phone}`} className="text-gray-300 hover:text-white flex items-center gap-1.5">
                <PhoneIcon /> {b.phone}
              </a>
            )}
            {b.email && (
              <a href={`mailto:${b.email}`} className="text-gray-300 hover:text-white flex items-center gap-1.5">
                <EmailIcon /> {b.email}
              </a>
            )}
            {b.rating && (
              <span className="text-yellow-400 flex items-center gap-1">
                ★ {b.rating} <span className="text-gray-500">({b.review_count?.toLocaleString()})</span>
              </span>
            )}
          </div>
        </div>

        {/* Links */}
        <div className="flex flex-col gap-2 shrink-0">
          <a
            href={b.gmb_url}
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs bg-blue-600 hover:bg-blue-500 text-white px-3 py-1.5 rounded-lg transition-colors text-center whitespace-nowrap"
          >
            Google Maps
          </a>
          {b.website ? (
            <a
              href={b.website}
              target="_blank"
              rel="noopener noreferrer"
              className="text-xs bg-gray-700 hover:bg-gray-600 text-gray-200 px-3 py-1.5 rounded-lg transition-colors text-center whitespace-nowrap"
            >
              Website
            </a>
          ) : (
            <span className="text-xs text-center text-green-400 font-medium px-3 py-1.5">Outreach target</span>
          )}
        </div>
      </div>
    </div>
  );
}

function PhoneIcon() {
  return (
    <svg className="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" />
    </svg>
  );
}

function EmailIcon() {
  return (
    <svg className="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
      <path strokeLinecap="round" strokeLinejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
    </svg>
  );
}
