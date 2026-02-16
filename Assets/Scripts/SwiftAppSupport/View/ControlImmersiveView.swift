//controlimmersiveview.swift
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

struct ControlImmersiveView: View {
    var appState: AppState
    @State private var currentPlaneValue: Float = 0.1
    // @State private var currentMaxPlaneValue: Int = 1 
    @State private var currentMaxSegmentValue: Int = 1
    @State private var hasPerformedSlice: Bool = false

    init(appState: AppState = AppState.shared) {
        appState.isGizmoVisible = false 
        self.appState = appState
    }

    var body: some View {
        @Bindable var state = appState
        ZStack {
            VStack (spacing: 20){
                HStack {
                    Spacer()
                    VStack(alignment: .trailing, spacing: 4) {
                        Text(state.selectedModel ?? "No model selected")
                            .font(.extraLargeTitle2)

                        if let side = state.selectedSide {
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
                                state.isRulerVisible.toggle() 
                                CallCSharpCallback("SetRulerVisibility", state.isRulerVisible ? 1 : 0)
                            }) {
                                Image(systemName: state.isRulerVisible ? "eye.fill" : "eye.slash.fill")
                                    .font(.system(size: 80))
                                    .foregroundColor(state.isRulerVisible ? .green : .red)
                            }
                        }
                        
                        HStack(spacing: 40) {
                            Image(systemName: "square.fill")
                                .font(.system(size: 80))

                            Button(action: {
                                state.isPlaneVisible.toggle() 
                                CallCSharpCallback("SetPlaneVisibility", state.isPlaneVisible ? 1 : 0)
                            }) {
                                Image(systemName: state.isPlaneVisible ? "eye.fill" : "eye.slash.fill")
                                    .font(.system(size: 80))
                                    .foregroundColor(state.isPlaneVisible ? .green : .red)
                            }
                        }
                        
                        
                        HStack(spacing: 40) {
                            Image(systemName: "rotate.3d") 
                                .font(.system(size: 80))

                            Button(action: {
                                state.isGizmoVisible.toggle() 
                                CallCSharpCallback("SetGizmoVisibility", state.isGizmoVisible ? 1 : 0)
                            }) {
                                Image(systemName: state.isGizmoVisible ? "eye.fill" : "eye.slash.fill")
                                    .font(.system(size: 80))
                                    .foregroundColor(state.isGizmoVisible ? .green : .red)
                            }
                        }
                    }

                    
                    VStack(alignment: .center, spacing: 60) {
                        Text("Set Plane Width")
                            .font(.title)
                            .fixedSize(horizontal: false, vertical: true) 

                        HStack(spacing: 20) {
                            Button(action: {
                                currentPlaneValue = max(0.1, currentPlaneValue - 0.05) 
                                CallCSharpCallback("SetPlaneScale", Int32(currentPlaneValue * 100))
                            }) {
                                Image(systemName: "minus.circle.fill")
                                    .font(.system(size: 80))
                            }
                            .buttonStyle(.plain)
                            .simultaneousGesture(LongPressGesture().onEnded { _ in
                                currentPlaneValue = 0.1 
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

                        //math maxplane =  max segment + 1
                        Text("Set Max Segment")
                            .font(.title)
                            .fixedSize(horizontal: false, vertical: true) 

                            HStack(spacing: 20) {
                            Button(action: {
                                currentMaxSegmentValue = max(1, currentMaxSegmentValue - 1)
                                CallCSharpCallback("SetMaxPlane", Int32(currentMaxSegmentValue + 1))
                            }) {
                                Image(systemName: "minus.circle.fill").font(.system(size: 80))
                            }

                            VStack {
                                Text("\(currentMaxSegmentValue)")
                                    .font(.system(size: 80, weight: .bold))
                                Text("(\(currentMaxSegmentValue + 1) Planes)")
                                    .font(.caption)
                            }
                            .frame(minWidth: 100)

                            Button(action: {
                                // max at 3
                                currentMaxSegmentValue = min(3, currentMaxSegmentValue + 1)
                                CallCSharpCallback("SetMaxPlane", Int32(currentMaxSegmentValue + 1))
                            }) {
                                Image(systemName: "plus.circle.fill").font(.system(size: 80))
                            }
                        }
                    }

                    
                    VStack(alignment: .trailing, spacing: 60) {

                        Button(hasPerformedSlice ? "Adjust" : "Slice") {
                            if hasPerformedSlice {
                                CallCSharpCallback("RevertToUncutModel")
                                hasPerformedSlice = false
                                state.isPlaneVisible = true 
                                state.isRulerVisible = true 
                                CallCSharpCallback("SetGizmoVisibility", state.isGizmoVisible ? 1 : 0)
                                CallCSharpCallback("SetPlaneVisibility", 1)
                                CallCSharpCallback("SetRulerVisibility", 1)
                            } else {
                                
                                CallCSharpCallback("TriggerSliceModel")
                                hasPerformedSlice = true
                                state.isPlaneVisible = false 
                                state.isRulerVisible = false 
                                state.isGizmoVisible = false
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
                            Image(systemName: state.isLocked ? "lock.fill" : "lock.open.fill") 
                                .font(.system(size: 60))
                                .foregroundColor(state.isLocked ? .yellow : .blue)

                            Button(action: {
                                state.isLocked.toggle() 
                                print("appState.isLocked: \(state.isLocked)")
                                CallCSharpCallback("SetLockPosition", state.isLocked ? 1 : 0)
                                
                                
                                let isVisible = state.isLocked ? 0 : 1
                                CallCSharpCallback("SetCylinderVisibility",Int32(isVisible))
                            }) {
                                Text(state.isLocked ? "Position Locked" : "Position Unlocked")
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