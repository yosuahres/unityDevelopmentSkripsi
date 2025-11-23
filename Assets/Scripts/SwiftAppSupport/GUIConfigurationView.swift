import SwiftUI
import RealityKit
import UnityFramework 
import PolySpatialRealityKit

enum SideSelection {
    case left, right
}

struct GUIConfigurationView: View {
    @State private var sideSelection: SideSelection? = nil
    @ObservedObject var appState: AppState

    init(appState: AppState = AppState.shared) {
        _appState = ObservedObject(wrappedValue: appState)
    }
    
    var body: some View {
        VStack {
            if let modelName = appState.selectedModel {
                
                if let resourceRoot = Bundle.main.resourceURL,
                   let url = URL(string: "Data/Raw/\(modelName)", relativeTo: resourceRoot) {
                    
                    Model3D(url: url) { model in
                        model
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                            .scaleEffect(0.5) 
                            .offset(y: -50)  
                    } placeholder: {
                        ProgressView("Loading \(modelName)...")
                    }
                    
                } else {
                    Text("Error: Could not find or access model file: \(modelName)")
                        .foregroundColor(.red)
                        .padding()
                }
                
            } else {
                Text("No model was selected from the Home screen.")
                    .foregroundColor(.secondary)
                    .padding()
            }
        }
        .onAppear {
            if let modelName = appState.selectedModel {
                CallCSharpCallback("LoadModel:\(modelName)")
            }
        }
        .toolbar {
            ToolbarItem(placement: .topBarLeading) {
                Button {
                    CallCSharpCallback("TriggerHomeScene")
                } label: {
                    Label("Return to Home", systemImage: "chevron.left")
                        .labelStyle(.iconOnly)
                }
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

                            // swapped because flipped gameobject in GUISlicing.cs
                            CallCSharpCallback("TriggerRight")
                            print("Continue tapped: Triggering Left")
                        } else if sideSelection == .right {
                            appState.selectedSide = "Right"
                            
                            CallCSharpCallback("TriggerLeft")
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
}