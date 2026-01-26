// SwiftUISampleInjectedScene.swift
import Foundation
import SwiftUI

struct SwiftUISampleInjectedScene {
    @SceneBuilder
    static var scene: some Scene {
        WindowGroup(id: "HomeView") {
            HomeView()
        }.defaultSize(width: 1600.0, height: 900.0)

        WindowGroup(id: "Configuration") {
            GUIConfigurationView()
        }

        WindowGroup(id: "ControlView") {
            ControlImmersiveView()
        }
    }
}

