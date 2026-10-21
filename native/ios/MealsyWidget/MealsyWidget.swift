import SwiftUI
import WidgetKit

struct ShoppingEntry: TimelineEntry { let date: Date }
struct ShoppingProvider: TimelineProvider {
    func placeholder(in context: Context) -> ShoppingEntry { ShoppingEntry(date: .now) }
    func getSnapshot(in context: Context, completion: @escaping (ShoppingEntry) -> Void) {
        completion(ShoppingEntry(date: .now))
    }
    func getTimeline(in context: Context, completion: @escaping (Timeline<ShoppingEntry>) -> Void) {
        completion(Timeline(entries: [ShoppingEntry(date: .now)], policy: .never))
    }
}

struct ShoppingWidgetView: View {
    let entry: ShoppingEntry
    private let green = Color(red: 0.13, green: 0.30, blue: 0.21)
    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            Label("Mealsy", systemImage: "basket.fill").font(.headline)
            Text("Remember it. Add it.").font(.caption)
            HStack(spacing: 20) {
                Link(destination: URL(string: "mealsy://widget/add")!) {
                    Image(systemName: "plus.circle.fill").font(.largeTitle)
                }.accessibilityLabel("Add to your selected shopping list")
                Link(destination: URL(string: "mealsy://widget/open")!) {
                    Image(systemName: "checklist").font(.title2)
                }.accessibilityLabel("Open your selected shopping list")
                Link(destination: URL(string: "mealsy://widget/configure")!) {
                    Image(systemName: "slider.horizontal.3").font(.title2)
                }.accessibilityLabel("Choose your shopping list")
            }
        }
        .foregroundStyle(green)
        .containerBackground(Color(red: 0.96, green: 0.97, blue: 0.95), for: .widget)
        .widgetURL(URL(string: "mealsy://widget/add")!)
    }
}

@main
struct MealsyShoppingWidget: Widget {
    let kind = "MealsyShoppingQuickAdd"
    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: ShoppingProvider()) { ShoppingWidgetView(entry: $0) }
            .configurationDisplayName("Quick shopping add")
            .description("Open quick add, your shopping list, or choose a personal or household list.")
            .supportedFamilies([.systemMedium])
    }
}
