//ControlImmersiveViw.swift
//mark 4 november
import SwiftUI
import RealityKit
import UnityFramework
import PolySpatialRealityKit

struct ControlImmersiveView: View {

    @State private var isRulerVisible: Bool = true
    
    // We removed the 'let rulers: [Entity]' property

    var body: some View {
        ZStack {
            HStack {
                Spacer()
                VStack( alignment: .trailing, spacing: 4) {
                    Text("should be model name")
                        .font(.extraLargeTitle2)
                }
            }
            .padding(.horizontal)

            //controls button
            HStack{
                VStack(alignment: .leading, spacing: 40) {

                    HStack(spacing: 40) {
                        Image(systemName: "ruler.fill")
                            .font(.system(size: 80))

                        Button(action: {
                            toggleRulerVisibility()
                        }) {
                            // This will still toggle the icon
                            Image(systemName: isRulerVisible ? "eye.fill" : "eye.slash.fill") 
                                .font(.system(size: 80))
                                .foregroundColor(isRulerVisible ? .green : .red)
                        }

                        Spacer()
                            .frame(width: 85)
                    }
                }
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
        
        // call event to unity
        
    }
}