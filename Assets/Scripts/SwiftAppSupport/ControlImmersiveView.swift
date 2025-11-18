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
            VStack (spacing: 20){
                HStack {
                    Spacer()
                    VStack(alignment: .trailing, spacing: 4) {
                        Text(appState.selectedModel ?? "No model selected")
                            .font(.extraLargeTitle2)
                        
                        if let side = appState.selectedSide {
                            
                            Text("\(side) Fragment")
                                .font(.title)
                                .foregroundColor(.secondary)
                            
                            Text("Side: \(side)")
                                .font(.title2)
                                .foregroundColor(.secondary)
                        }
                        
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
                        Button("Return") {
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
    }


    func setRulersVisibility(_ visible: Bool) {
        isRulerVisible = visible
    }

    func toggleRulerVisibility() {
        isRulerVisible.toggle()
        setRulersVisibility(isRulerVisible)
        
    }
}
