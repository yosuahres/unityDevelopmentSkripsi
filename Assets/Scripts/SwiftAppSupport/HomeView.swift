//homeview.swift
//mark 1 november
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

struct HomeView: View {
    
    @State private var models: [String] = []
    @State private var selection: String? = nil
    @State private var loadedModel: ModelEntity? = nil
    @State private var errorMessage: String? = nil
    @State private var searchText: String = ""
    @ObservedObject var appState: AppState   

    init(appState: AppState = AppState.shared) {
        _appState = ObservedObject(wrappedValue: appState)
    }
    
    var filteredCaseGroups: [String] {
        if searchText.isEmpty  {
            return models
        } else {
            return models.filter{$0.localizedCaseInsensitiveContains(searchText)}
        }
    }

    var body: some View {
        NavigationSplitView {
            List(filteredCaseGroups, id: \.self, selection: $selection) { modelName in
                HStack(spacing: 8){
                    Image("glyph")
                        .resizable()
                        .frame(width: 50, height: 50)
                    VStack(alignment: .leading) {
                        Text(modelName)
                            .lineLimit(1)
                            .foregroundStyle(.secondary)
                    }
                }
            }
            .navigationTitle("Session")
            .onAppear(perform: loadModelList)
            .searchable(text: $searchText, prompt: "Search groups")
            .onChange(of: selection) { newSelection in
                // update shared AppState so other SwiftUI views can observe the selection
                appState.selectedModel = newSelection
            }
            
        } detail: {
            VStack {
                if let model = self.loadedModel {
                    RealityView { content in
                        let root = Entity()
                        root.addChild(model)
                        content.add(root)
                        
                        let bounds = model.visualBounds(relativeTo: model)
                        if bounds.extents == .zero {
                            print("WARNING: Model bounds are zero. Model may be empty.")
                            model.position = [0, 0, 0]
                        } else {
                            let maxDimension = max(bounds.extents.x, max(bounds.extents.y, bounds.extents.z))
                            let targetSize: Float = 0.5
                            let scale = targetSize / maxDimension
                            
                            model.scale = [scale, scale, scale]
                            model.position = -bounds.center * scale
                        }
                        
                        root.position = [0, 0, -0.75]
                    }
                    
                } else if let errorMessage = self.errorMessage {
                    Text("Error loading model: \(errorMessage)")
                        .foregroundColor(.red)
                        .padding()
                    
                } else if selection != nil {
                    ProgressView("Loading \(selection!)...")
                    
                } else {
                    Text("No Object Selected")
                        .foregroundColor(.secondary)
                }
            }
            .task(id: selection) {
                await MainActor.run {
                    loadedModel = nil
                    errorMessage = nil
                }

                if let modelName = selection {
                    await loadModel(named: modelName)
                }
            }
        }

        .frame(minWidth: 800, minHeight: 300)

        .toolbar {
            ToolbarItem(placement: .bottomOrnament) {
                VStack {
                    if selection != nil {
                        Button("Go to Immersive Space") {
                            CallCSharpCallback("TriggerImmersiveScene")
                        }
                        .disabled(selection == nil)
                    }
                }
            }
        }
    }
    
    func loadModelList() {
        var found: [String] = []
        if let urls = Bundle.main.urls(forResourcesWithExtension: "usdz", subdirectory: "Data/Raw") {
            found.append(contentsOf: urls.map { $0.lastPathComponent })
        }
        
        self.models = Array(Set(found)).sorted()
    }
    
    func loadModel(named modelName: String) async {
        do {
            guard let resourceRoot = Bundle.main.resourceURL,
                  let url = URL(string: "Data/Raw/\(modelName)", relativeTo: resourceRoot)
            else {
                throw URLError(.fileDoesNotExist, userInfo: [NSLocalizedDescriptionKey: "Could not find \(modelName) in Data/Raw."])
            }
            
            guard FileManager.default.fileExists(atPath: url.path) else {
                throw URLError(.fileDoesNotExist, userInfo: [NSLocalizedDescriptionKey: "\(modelName) not found at \(url.path)"])
            }

            let loadedEntity = try await ModelEntity.loadModel(contentsOf: url)

            await MainActor.run {
                self.loadedModel = loadedEntity
            }
        } catch {
            await MainActor.run {
                self.errorMessage = error.localizedDescription
                print("Error loading model '\(modelName)': \(error.localizedDescription)")
            }
        }
    }
}
