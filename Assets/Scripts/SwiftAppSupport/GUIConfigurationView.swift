import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

enum SideSelection {
    case left, right
}

struct GUIConfigurationView: View {
    
    @State private var loadedModel: ModelEntity? = nil
    @State private var errorMessage: String? = nil
    @State private var sideSelection: SideSelection? = nil
    @ObservedObject var appState: AppState

    init(appState: AppState = AppState.shared) {
        _appState = ObservedObject(wrappedValue: appState)
    }
    
    var body: some View {
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
                
            } else {
                ProgressView("Loading \(appState.selectedModel ?? "model")...")
            }
        }
        .task(id: appState.selectedModel) {
            await MainActor.run {
                loadedModel = nil
                errorMessage = nil
            }

            if let modelName = appState.selectedModel {
                CallCSharpCallback("LoadModel:\(modelName)")
                await loadModel(named: modelName)
            } else {
                self.errorMessage = "No model was selected from the Home screen."
            }
        }
        .toolbar {
            ToolbarItem(placement: .bottomOrnament) {
                VStack(spacing: 12) {
                    HStack {
                        Button("Left") {
                            sideSelection = .left
                            print("Left side selected")
                        }
                        .font(.title2)
                        .controlSize(.large)
                        .padding(.horizontal, 20)
                        .padding(.vertical, 10)
                        .glassBackgroundEffect(
                            in: .rect(cornerRadius: 10),
                            displayMode: sideSelection == .left ? .always : .implicit
                        )
                        
                        Button("Right") {
                            sideSelection = .right
                            print("Right side selected")
                        }
                        .font(.title2)
                        .controlSize(.large)
                        .padding(.horizontal, 20)
                        .padding(.vertical, 10)
                        .glassBackgroundEffect(
                            in: .rect(cornerRadius: 10),
                            displayMode: sideSelection == .right ? .always : .implicit
                        )
                    }
                    
                    Button("Continue") {
                        if sideSelection == .left {
                            appState.selectedSide = "Left"

                            CallCSharpCallback("TriggerLeft")
                            print("Continue tapped: Triggering Left")
                        } else if sideSelection == .right {
                            appState.selectedSide = "Right"
                            
                            CallCSharpCallback("TriggerRight")
                            print("Continue tapped: Triggering Right")
                        }
                    }
                    .font(.title)
                    .controlSize(.large)
                    .padding(.horizontal, 30)
                    .padding(.vertical, 15)
                    .disabled(sideSelection == nil)
                }
            }
        }
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
