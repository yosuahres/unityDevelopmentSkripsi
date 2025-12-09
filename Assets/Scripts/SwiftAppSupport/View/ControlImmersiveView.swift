//controlimmersiveview.swift
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

struct ControlImmersiveView: View {

    @ObservedObject var appState: AppState
    @State private var currentPlaneValue: Float = 0.2
    @State private var currentMaxPlaneValue: Int = 3
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


            VStack {
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
                        
                        
                        HStack(spacing: 40) {
                            
                            Image(systemName: "rotate.3d") 
                                .font(.system(size: 80))

                            Button(action: {
                                appState.isGizmoVisible.toggle()
                                
                                CallCSharpCallback("SetGizmoVisibility", appState.isGizmoVisible ? 1 : 0)
                            }) {
                                Image(systemName: appState.isGizmoVisible ? "eye.fill" : "eye.slash.fill")
                                    .font(.system(size: 80))
                                    .foregroundColor(appState.isGizmoVisible ? .green : .red)
                            }
                        }
                    }


                    VStack(alignment: .center, spacing: 60) {
                        
                        
                        Text("Set Plane Width")
                            .font(.title)

                        HStack(spacing: 20) {
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

                            Text(String(format: "%.2f", currentPlaneValue))
                                .font(.system(size: 50, weight: .bold))
                                .frame(minWidth: 100)

                            Button(action: {
                                currentPlaneValue = min(0.5, currentPlaneValue + 0.05)
                                CallCSharpCallback("SetPlaneScale", Int32(currentPlaneValue * 100))
                            }) {
                                Image(systemName: "plus.circle.fill")
                                    .font(.system(size: 80))
                            }
                            .buttonStyle(.plain)
                        }

                        
                        Text("Set Max Planes")
                            .font(.title)

                        HStack(spacing: 20) {
                            Button(action: {
                                currentMaxPlaneValue = max(1, currentMaxPlaneValue - 1)
                                CallCSharpCallback("SetMaxPlane", Int32(currentMaxPlaneValue))
                            }) {
                                Image(systemName: "minus.circle.fill")
                                    .font(.system(size: 80))
                            }
                            .buttonStyle(.plain)
                            .simultaneousGesture(LongPressGesture().onEnded { _ in
                                currentMaxPlaneValue = 3
                                CallCSharpCallback("SetMaxPlane", Int32(currentMaxPlaneValue))
                            })

                            Text("\(currentMaxPlaneValue)")
                                .font(.system(size: 80, weight: .bold))
                                .frame(minWidth: 100)

                            Button(action: {
                                currentMaxPlaneValue = min(5, currentMaxPlaneValue + 1)
                                CallCSharpCallback("SetMaxPlane", Int32(currentMaxPlaneValue))
                            }) {
                                Image(systemName: "plus.circle.fill")
                                    .font(.system(size: 80))
                            }
                            .buttonStyle(.plain)
                        }
                    }


                    VStack(alignment: .trailing, spacing: 60) {

                        Button(hasPerformedSlice ? "Adjust" : "Slice") {
                            if hasPerformedSlice {
                                CallCSharpCallback("RevertToUncutModel")
                                hasPerformedSlice = false
                                appState.isPlaneVisible = true
                                appState.isRulerVisible = true
                                
                                appState.isGizmoVisible = true 
                                CallCSharpCallback("SetPlaneVisibility", 1)
                                CallCSharpCallback("SetRulerVisibility", 1)
                                CallCSharpCallback("SetGizmoVisibility", 1) 
                            } else {
                                CallCSharpCallback("TriggerSliceModel")
                                hasPerformedSlice = true
                                appState.isPlaneVisible = false
                                appState.isRulerVisible = false
                                
                                appState.isGizmoVisible = false 
                                CallCSharpCallback("SetGizmoVisibility", 0) 
                            }
                        }
                        .font(.system(size: 80))
                        .fontWeight(.bold)
                        .padding(30)
                        .buttonStyle(.borderedProminent)
                        .controlSize(.extraLarge)
                        .hoverEffect()


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
                }
                .padding(.horizontal, 50)

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