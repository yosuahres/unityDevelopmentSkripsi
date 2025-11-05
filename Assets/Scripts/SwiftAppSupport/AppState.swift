import Foundation
import Combine
import PolySpatialRealityKit
import SwiftUI
import RealityKit
import UnityFramework

final class AppState: ObservableObject {

    @Published var immersiveSpaceState = ImmersiveSpaceState.closed
    @Published var controlsWindowState: WindowState = .closed
    @Published var selectedModel: String? = nil

    enum ImmersiveSpaceState {
        case closed
        case inTransition
        case open
    }

    static let shared = AppState()
    private init() {}

    func didLeaveImmersiveSpace() {
        immersiveSpaceState = .closed
    }

    func openControlsWindow(openWindow: OpenWindowAction, dismissWindow: DismissWindowAction) async {
        dismissWindow(id: "controls")
        
        controlsWindowState = .opening
        openWindow(id: "controls")
        controlsWindowState = .open
    }
    
    func closeControlsWindow(dismissWindow: DismissWindowAction) async {
        dismissWindow(id: "controls")
        controlsWindowState = .closed
        resetCaseState()
    }

}
