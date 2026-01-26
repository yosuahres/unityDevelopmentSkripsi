//guiconfigurationview.swift
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

enum SideSelection {
    case left, right
}

struct GUIConfigurationView: View {
    @State private var sideSelection: SideSelection? = nil
    var appState: AppState

    init(appState: AppState = AppState.shared) {
        self.appState = appState
    }
    
    var modelURL: URL? {
        guard let modelName = appState.selectedModel,
              let resourceRoot = Bundle.main.resourceURL else {
            return nil
        }
        
        return URL(string: "Data/Raw/\(modelName)", relativeTo: resourceRoot)
    }

    var body: some View {
        @Bindable var state = appState
        
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
                if let modelName = state.selectedModel, let url = modelURL {
                    Model3D(url: url) { model in
                        model
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                            .scaleEffect(0.5)
                            .offset(y: -50)
                    } placeholder: {
                        ProgressView("Loading \(modelName)...")
                    }

                } else if state.selectedModel != nil {
                    Text("Error: Could not find or access model file: \(state.selectedModel!)")
                        .foregroundColor(.red)
                        .padding()
                }
                else {
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
            if let modelName = state.selectedModel { 
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
                            state.selectedSide = "Left" 
                            CallCSharpCallback("TriggerLeft")
                        } else if sideSelection == .right {
                            state.selectedSide = "Right" 
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