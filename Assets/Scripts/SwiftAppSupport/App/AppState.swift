//appstate.swift
import Foundation
import Combine 
import PolySpatialRealityKit
import SwiftUI
import RealityKit
import UnityFramework

@Observable 
final class AppState { 
    var selectedModel: String? = nil
    var selectedSide: String? = nil
    var isPlaneVisible: Bool = true  
    var isRulerVisible: Bool = true
    var isGizmoVisible: Bool = false
    var isLocked: Bool = false

    static let shared = AppState()
    private init() {}
}