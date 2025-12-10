//appstate.swift
import Foundation
import Combine
import PolySpatialRealityKit
import SwiftUI
import RealityKit
import UnityFramework

final class AppState: ObservableObject {

    @Published var selectedModel: String? = nil
    
    @Published var selectedSide: String? = nil
    @Published var isPlaneVisible: Bool = true  
    @Published var isRulerVisible: Bool = true
    @Published var isGizmoVisible: Bool = false
    @Published var isLocked: Bool = false

    static let shared = AppState()
    private init() {}

}