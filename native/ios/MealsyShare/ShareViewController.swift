import UIKit
import UniformTypeIdentifiers

// No Firebase SDK, credentials or network calls in the extension.
final class ShareViewController: UIViewController {
    private let status = UILabel()
    private let save = UIButton(type: .system)
    private var recipeURL: URL?
    private var saving = false
    private let group = "group.com.companyname.recipeapp"

    override func viewDidLoad() {
        super.viewDidLoad()
        view.backgroundColor = .systemBackground
        preferredContentSize = CGSize(width: 360, height: 260)
        let title = UILabel()
        title.text = "Share to Mealsy"; title.font = .preferredFont(forTextStyle: .title2)
        status.numberOfLines = 0; status.text = "Reading recipe link…"
        status.font = .preferredFont(forTextStyle: .body)
        save.setTitle("Save link for review", for: .normal); save.isEnabled = false
        save.addTarget(self, action: #selector(saveLink), for: .touchUpInside)
        let cancel = UIButton(type: .system)
        cancel.setTitle("Cancel", for: .normal)
        cancel.addTarget(self, action: #selector(close), for: .touchUpInside)
        let stack = UIStackView(arrangedSubviews: [title, status, save, cancel])
        stack.axis = .vertical; stack.spacing = 16; stack.translatesAutoresizingMaskIntoConstraints = false
        view.addSubview(stack)
        NSLayoutConstraint.activate([
            stack.leadingAnchor.constraint(equalTo: view.safeAreaLayoutGuide.leadingAnchor, constant: 24),
            stack.trailingAnchor.constraint(equalTo: view.safeAreaLayoutGuide.trailingAnchor, constant: -24),
            stack.topAnchor.constraint(equalTo: view.safeAreaLayoutGuide.topAnchor, constant: 24)
        ])
        Task { @MainActor in await loadLink() }
    }

    @MainActor private func loadLink() async {
        let items = extensionContext?.inputItems as? [NSExtensionItem] ?? []
        for item in items {
            for provider in item.attachments ?? [] {
                guard provider.hasItemConformingToTypeIdentifier(UTType.url.identifier) else { continue }
                let value: NSSecureCoding? = await withCheckedContinuation { continuation in
                    provider.loadItem(forTypeIdentifier: UTType.url.identifier, options: nil) { value, _ in
                        continuation.resume(returning: value)
                    }
                }
                guard let url = value as? URL, url.scheme?.lowercased() == "https",
                      url.host != nil, url.user == nil, url.password == nil,
                      url.port == nil || url.port == 443, url.absoluteString.utf8.count <= 2000 else { continue }
                recipeURL = url; save.isEnabled = true
                status.text = "Save this link, then open Mealsy to review it. Importing requires Premium and a supported recipe website.\n\n" + (url.host ?? "")
                return
            }
        }
        status.text = "Share the HTTPS recipe page from Safari or your browser. Photos and files cannot be imported here."
    }

    @objc private func saveLink() {
        guard !saving, let url = recipeURL else { return }
        saving = true; save.isEnabled = false
        do {
            guard let container = FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: group) else {
                throw NSError(domain: "MealsyShare", code: 1)
            }
            let inbox = container.appendingPathComponent("RecipeInbox", isDirectory: true)
            try FileManager.default.createDirectory(at: inbox, withIntermediateDirectories: true)
            let pending = try FileManager.default.contentsOfDirectory(at: inbox, includingPropertiesForKeys: nil)
                .filter { $0.pathExtension == "txt" }
            guard pending.count < 20 else {
                status.text = "Open Mealsy to review your saved links before sharing more."
                saving = false; save.isEnabled = true; return
            }
            try Data(url.absoluteString.utf8).write(to: inbox.appendingPathComponent(UUID().uuidString + ".txt"), options: .atomic)
            status.text = "Link saved. Open Mealsy to review and import it. Nothing has been downloaded or added to your recipes yet."
            save.setTitle("Saved", for: .normal)
            // Share extensions cannot reliably launch their containing app.
            // Let the user read confirmation and close using the Done button.
            if let stack = save.superview as? UIStackView,
               let cancel = stack.arrangedSubviews.last as? UIButton { cancel.setTitle("Done", for: .normal) }
        } catch {
            status.text = "Couldn't save the link. Check the Mealsy App Group setup, or copy the link into Mealsy's recipe importer."
            saving = false; save.isEnabled = true
        }
    }
    @objc private func close() { extensionContext?.completeRequest(returningItems: nil) }
}
