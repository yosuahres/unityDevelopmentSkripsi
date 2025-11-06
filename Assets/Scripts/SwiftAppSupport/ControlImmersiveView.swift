//ControlImmersiveViw.swift
//mark 4 november
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

struct ControlImmersiveView: View {

    @State private var isRulerVisible: Bool = true
    @ObservedObject var appState: AppState   

    init(appState: AppState = AppState.shared) {
        _appState = ObservedObject(wrappedValue: appState)
    }

    var body: some View {
        ZStack {
            // Top-right model name
            VStack (spacing: 20){
                HStack {
                    Spacer()
                    VStack(alignment: .trailing, spacing: 4) {
                        Text(appState.selectedModel ?? "No model selected")
                            .font(.extraLargeTitle2)
                    }
                    .padding(.horizontal)
                }
                Spacer()
            }

            // Centered controls (ruler + eye toggle)
            HStack {
                VStack (alignment: .leading, spacing: 40) {
                    Spacer()
                    HStack {
                        Spacer()

                        HStack(spacing: 40) {
                            Image(systemName: "ruler.fill")
                                .font(.system(size: 80))

                            Button(action: {
                                toggleRulerVisibility()
                            }) {
                                Image(systemName: isRulerVisible ? "eye.fill" : "eye.slash.fill")
                                    .font(.system(size: 80))
                                    .foregroundColor(isRulerVisible ? .green : .red)
                            }
                        }
                        Spacer()

                        HStack(spacing:40) {
                            //toggle
                        }


                    }
                    Spacer()
                    .padding(.horizontal)
                    Spacer()

                    HStack{
                        Button("Close") {
                            CallCSharpCallback("TriggerHomeScene")
                        }
                        .font(.system(size: 80))
                        .fontWeight(.bold)
                        .padding(50)
                        .buttonStyle(.borderedProminent)
                        .controlSize(.extraLarge)
                        .hoverEffect()
                        Spacer()

                        Button("Slice") {
                            CallCSharpCallback("TriggerSliceModel")
                        }
                        .font(.system(size: 80))
                        .fontWeight(.bold)
                        .padding(50)
                        .buttonStyle(.borderedProminent)
                        .controlSize(.extraLarge)
                        .hoverEffect()
                        Spacer()
                    }
                }
            }
        }
        .onAppear {
            // If a model is already selected when this view appears, tell Unity to load it
            if let name = appState.selectedModel {
                let base = (name as NSString).deletingPathExtension
                CallCSharpCallback("LoadModel:\(base)")
            }
        }
        .onChange(of: appState.selectedModel) { newName in
            // send the new selection to Unity (strip extension)
            if let name = newName {
                let base = (name as NSString).deletingPathExtension
                CallCSharpCallback("LoadModel:\(base)")
            }
        }
    }


    func setRulersVisibility(_ visible: Bool) {
        // --- We commented out the part that needs the real rulers ---
        // for ruler in rulers {
        //     ruler.isEnabled = visible
        // }
        isRulerVisible = visible
    }

    func toggleRulerVisibility() {
        isRulerVisible.toggle()
        setRulersVisibility(isRulerVisible)
        
        // call event to unity if needed
    }
}