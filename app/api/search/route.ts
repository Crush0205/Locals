import { NextRequest, NextResponse } from "next/server";

const GOOGLE_API_KEY = process.env.GOOGLE_PLACES_API_KEY;

const SERVICE_CATEGORIES = [
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

export interface Business {
  place_id: string;
  name: string;
  address: string;
  phone: string | null;
  website: string | null;
  gmb_url: string;
  rating: number | null;
  review_count: number | null;
  category: string;
  has_website: boolean;
  email: string | null;
  status: string;
}

async function searchPlaces(query: string, city: string): Promise<Business[]> {
  if (!GOOGLE_API_KEY) throw new Error("GOOGLE_PLACES_API_KEY is not configured");

  const searchQuery = `${query} in ${city}`;
  const url = new URL("https://maps.googleapis.com/maps/api/place/textsearch/json");
  url.searchParams.set("query", searchQuery);
  url.searchParams.set("key", GOOGLE_API_KEY);
  url.searchParams.set("type", "establishment");

  const res = await fetch(url.toString());
  if (!res.ok) throw new Error(`Places API error: ${res.status}`);
  const data = await res.json();

  if (data.status !== "OK" && data.status !== "ZERO_RESULTS") {
    throw new Error(`Places API returned: ${data.status} — ${data.error_message || ""}`);
  }

  const results: Business[] = [];

  for (const place of (data.results || []).slice(0, 5)) {
    const detail = await getPlaceDetail(place.place_id);
    if (!detail) continue;

    const hasWebsite = !!detail.website;
    const gmbUrl = `https://www.google.com/maps/place/?q=place_id:${place.place_id}`;

    results.push({
      place_id: place.place_id,
      name: detail.name || place.name,
      address: detail.formatted_address || place.formatted_address || "",
      phone: detail.formatted_phone_number || null,
      website: detail.website || null,
      gmb_url: gmbUrl,
      rating: detail.rating || place.rating || null,
      review_count: detail.user_ratings_total || place.user_ratings_total || null,
      category: query,
      has_website: hasWebsite,
      email: null,
      status: detail.business_status || place.business_status || "OPERATIONAL",
    });
  }

  return results;
}

async function getPlaceDetail(placeId: string) {
  const url = new URL("https://maps.googleapis.com/maps/api/place/details/json");
  url.searchParams.set("place_id", placeId);
  url.searchParams.set("fields", "name,formatted_address,formatted_phone_number,website,rating,user_ratings_total,business_status");
  url.searchParams.set("key", GOOGLE_API_KEY!);

  const res = await fetch(url.toString());
  if (!res.ok) return null;
  const data = await res.json();
  return data.result || null;
}

export async function GET(req: NextRequest) {
  const city = req.nextUrl.searchParams.get("city")?.trim();
  const categoriesParam = req.nextUrl.searchParams.get("categories");
  const noWebsiteOnly = req.nextUrl.searchParams.get("no_website") === "true";

  if (!city) {
    return NextResponse.json({ error: "city is required" }, { status: 400 });
  }

  if (!GOOGLE_API_KEY) {
    return NextResponse.json(
      { error: "Google Places API key not configured. Add GOOGLE_PLACES_API_KEY to your .env.local file." },
      { status: 503 }
    );
  }

  const categories = categoriesParam
    ? categoriesParam.split(",").map((c) => c.trim()).filter(Boolean)
    : SERVICE_CATEGORIES.slice(0, 8);

  const allResults: Business[] = [];
  const seenIds = new Set<string>();

  for (const category of categories) {
    try {
      const businesses = await searchPlaces(category, city);
      for (const b of businesses) {
        if (!seenIds.has(b.place_id)) {
          seenIds.add(b.place_id);
          allResults.push(b);
        }
      }
      await new Promise((r) => setTimeout(r, 200));
    } catch (err) {
      console.error(`Error searching ${category}:`, err);
    }
  }

  const filtered = noWebsiteOnly ? allResults.filter((b) => !b.has_website) : allResults;

  return NextResponse.json({ businesses: filtered, total: filtered.length, city });
}
