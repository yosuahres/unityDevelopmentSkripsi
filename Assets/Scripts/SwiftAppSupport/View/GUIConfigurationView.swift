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
            HStack {
                
                
                VStack {
                    Button {
                        CallCSharpCallback("TriggerHomeScene")
                    } label: {
                        Image(systemName: "chevron.left")
                            .font(.largeTitle)
                    }
                    .padding(.top, 20)
                    .padding(.leading, 0)
                    Spacer()
                }
                .padding(.leading, 20)


                VStack(alignment: .leading) {
                    Text("RIGHT SIDE")
                        .font(.title2)
                        .lineLimit(nil)
                        .padding(.top, 20)
                        .padding(.leading, 20)
                    Spacer()
                }
                
                Spacer() 
                
                
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
                
                Spacer() 

                VStack(alignment: .trailing) {
                    Text("LEFT SIDE")
                        .font(.title2)
                        .lineLimit(nil)
                        .padding(.top, 20)
                        .padding(.trailing, 20)
                    Spacer()
                }
            }
            
            
        }
        .onAppear {
            if let modelName = appState.selectedModel {
                CallCSharpCallback("LoadModel:\(modelName)")
            }
        }
        .toolbar {
            ToolbarItem(placement: .bottomOrnament) {
                
                VStack(spacing: 12) {
                    HStack {
                        Button("Right") {
                            sideSelection = .right
                        }
                        .font(.title2)
                        .controlSize(.large)
                        .padding(.horizontal, 20)
                        .padding(.vertical, 10)
                        .glassBackgroundEffect(
                            in: .rect(cornerRadius: 10),
                            displayMode: sideSelection == .right ? .always : .implicit
                        )
                        
                        Button("Left") {
                            sideSelection = .left
                        }
                        .font(.title2)
                        .controlSize(.large)
                        .padding(.horizontal, 20)
                        .padding(.vertical, 10)
                        .glassBackgroundEffect(
                            in: .rect(cornerRadius: 10),
                            displayMode: sideSelection == .left ? .always : .implicit
                        )
                    }
                    
                    Button("Continue") {
                        if sideSelection == .left {
                            appState.selectedSide = "Left"
                            
                            CallCSharpCallback("TriggerLeft")
                        } else if sideSelection == .right {
                            appState.selectedSide = "Right"
                            CallCSharpCallback("TriggerRight")
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
