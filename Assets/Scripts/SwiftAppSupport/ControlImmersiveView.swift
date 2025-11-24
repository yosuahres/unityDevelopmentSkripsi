import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

struct ControlImmersiveView: View {

    @ObservedObject var appState: AppState   
    @State private var currentValue: Int = 50

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
                            
                            // Text("\(side) Fragment")
                            //     .font(.title)
                            //     .foregroundColor(.secondary)
                            
                            Text("Side: \(side)")
                                .font(.title2)
                                .foregroundColor(.secondary)
                        }
                        
                    }
                    .padding(.horizontal)
                }
                Spacer()
            }

            HStack {
                VStack (alignment: .leading, spacing: 40) {
                    Spacer()
                    
                    HStack(alignment: .top, spacing: 80) { 
                        
                        VStack(alignment: .leading, spacing: 60) {
                            HStack(spacing: 40) {
                                Image(systemName: "ruler.fill")
                                    .font(.system(size: 80))

                                Button(action: {
                                    appState.isRulerVisible.toggle()
                                }) {
                                    Image(systemName: appState.isRulerVisible ? "eye.fill" : "eye.slash.fill")
                                        .font(.system(size: 80))
                                        .foregroundColor(appState.isRulerVisible ? .green : .red)
                                }
                            }

                            HStack(spacing: 40) {
                                Image(systemName: "square.fill")
                                    .font(.system(size: 80))

                                Button(action: {
                                    // Placeholder action for square/rectangle visibility toggle
                                }) {
                                    Image(systemName: appState.isRulerVisible ? "eye.fill" : "eye.slash.fill")
                                        .font(.system(size: 80))
                                        .foregroundColor(appState.isRulerVisible ? .green : .red)
                                }
                            }
                        }
                        
                        // RIGHT COLUMN
                        VStack(alignment: .leading, spacing: 60) {
                            HStack(spacing: 20) {
                                Button(action: {
                                    if currentValue > 0 {
                                        currentValue -= 1
                                    }
                                }) {
                                    Image(systemName: "minus.circle.fill")
                                        .font(.system(size: 80))
                                }
                                .buttonStyle(.plain)

                                Text("\(currentValue)")
                                    .font(.system(size: 80, weight: .bold))
                                    .frame(minWidth: 100)
                                
                                Button(action: {
                                    if currentValue < 100 { 
                                        currentValue += 1
                                    }
                                }) {
                                    Image(systemName: "plus.circle.fill")
                                        .font(.system(size: 80))
                                }
                                .buttonStyle(.plain)
                            }
                            
                            Button("Slice") {
                                CallCSharpCallback("TriggerSliceModel")
                            }
                            .font(.system(size: 80))
                            .fontWeight(.bold)
                            .padding(50)
                            .buttonStyle(.borderedProminent)
                            .controlSize(.extraLarge)
                            .hoverEffect()
                        }
                        
                        Spacer() 
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
                    }
                }
            }
        }
    }
}