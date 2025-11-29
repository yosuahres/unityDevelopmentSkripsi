//controlimmersiveview.swift
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

struct ControlImmersiveView: View {

    @ObservedObject var appState: AppState
    @State private var currentPlaneValue: Float = 0.2
    // State to track if the slice has been performed
    @State private var hasPerformedSlice: Bool = false 

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
                                    CallCSharpCallback("SetRulerVisibility", appState.isRulerVisible ? 1 : 0)
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
                                    appState.isPlaneVisible.toggle()
                                    CallCSharpCallback("SetPlaneVisibility", appState.isPlaneVisible ? 1 : 0)
                                }) {
                                    Image(systemName: appState.isPlaneVisible ? "eye.fill" : "eye.slash.fill")
                                        .font(.system(size: 80))
                                        .foregroundColor(appState.isPlaneVisible ? .green : .red)
                                }
                            }
                        }

                        // RIGHT COLUMN
                        VStack(alignment: .leading, spacing: 60) {
                            HStack(spacing: 20) {
                                VStack(spacing: 30) { 
                                    
                                    // Slice/Toggle Button
                                    Button(hasPerformedSlice ? "Adjust" : "Slice") {
                                        if hasPerformedSlice {
                                            // Revert (Adjust mode): Revert model, show planes
                                            CallCSharpCallback("RevertToUncutModel")
                                            hasPerformedSlice = false // Go back to Slice mode
                                            
                                            // ⭐ CRITICAL FIX: Explicitly set visibility to true via C# callback
                                            appState.isPlaneVisible = true 
                                            appState.isRulerVisible = true 
                                            CallCSharpCallback("SetPlaneVisibility", 1) 
                                            CallCSharpCallback("SetRulerVisibility", 1)
                                        } else {
                                            // Perform slice: Cut model, hide planes
                                            CallCSharpCallback("TriggerSliceModel")
                                            hasPerformedSlice = true // Go to Adjust mode
                                            
                                            // Visibility is handled inside PerformOsteotomySlice (SetPlaneVisibility(false))
                                            appState.isPlaneVisible = false 
                                            appState.isRulerVisible = false
                                        }
                                    }
                                    .font(.system(size: 80))
                                    .fontWeight(.bold)
                                    .padding(30)
                                    .buttonStyle(.borderedProminent)
                                    .controlSize(.extraLarge)
                                    .hoverEffect()

                                    //lock position
                                    HStack(spacing: 40) {
                                        Image(systemName: appState.isLocked ? "lock.fill" : "lock.open.fill")
                                            .font(.system(size: 60))
                                            .foregroundColor(appState.isLocked ? .yellow : .blue)

                                        Button(action: {
                                            appState.isLocked.toggle()
                                            print("appState.isLocked: \(appState.isLocked)")
                                            CallCSharpCallback("SetLockPosition", appState.isLocked ? 1 : 0)
                                        }) {
                                            Text(appState.isLocked ? "Position Locked" : "Position Unlocked")
                                                .font(.system(size: 40))
                                                .padding(20)
                                        }
                                        .buttonStyle(.bordered)
                                        .hoverEffect()
                                    }
                                }
                                .padding(.bottom, 30)

                                Button(action: {
                                    currentPlaneValue = max(0.2, currentPlaneValue - 0.05)
                                    CallCSharpCallback("SetPlaneScale", Int32(currentPlaneValue * 100))
                                }) {
                                    Image(systemName: "minus.circle.fill")
                                        .font(.system(size: 80))
                                }
                                .buttonStyle(.plain)
                                .simultaneousGesture(LongPressGesture().onEnded { _ in
                                    currentPlaneValue = 0.2
                                    CallCSharpCallback("SetPlaneScale", Int32(currentPlaneValue * 100))
                                })

                                Text(String(format: "%.3f", currentPlaneValue))
                                    .font(.system(size: 80, weight: .bold))
                                    .frame(minWidth: 100)

                                // Plus button
                                Button(action: {
                                    currentPlaneValue = min(0.5, currentPlaneValue + 0.05)
                                    CallCSharpCallback("SetPlaneScale", Int32(currentPlaneValue * 100))
                                }) {
                                    Image(systemName: "plus.circle.fill")
                                        .font(.system(size: 80))
                                }
                                .buttonStyle(.plain)
                            }

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