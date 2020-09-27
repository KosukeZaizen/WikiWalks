import * as React from 'react';
import "./animation.css"
import runningNinja from './ninja_hashiru.png';
import shuriken from './shuriken.png';

export default class WelcomeAnimation extends React.Component {

    constructor(props) {
        super(props);
        this.state = {
            isShown: true,
        };
    }

    componentDidMount = () => {
        setTimeout(() => {
            this.setState({ isShown: false });
        }, 4350);
    }

    render() {
        const innerWidth = window.innerWidth;
        const innerHeight = window.innerHeight;
        const squareLength = innerWidth < innerHeight ? innerWidth : innerHeight;
        const leftTopPosition = [(innerWidth - squareLength) / 2, (innerHeight - squareLength) / 2];
        const U = squareLength / 1000; // unit length

        const charHeight = 130 * U;
        const charTop = leftTopPosition[1] + ((squareLength - charHeight) * (2 / 5));

        return (
            this.state.isShown &&
            <div
                style={{
                    width: innerWidth,
                    height: innerHeight,
                    position: "fixed",
                    top: 0,
                    left: 0,
                    zIndex: 9999999999,
                    backgroundColor: "white",
                }}
                className="screen"
            >
                <p
                    style={{
                        width: 1000 * U,
                        position: "absolute",
                        left: leftTopPosition[0],
                        top: charTop,
                        textAlign: "center",
                        fontSize: charHeight,
                        padding: 0,
                        fontWeight: "bold",
                        fontStyle: "italic",
                    }}
                    className="logoChar"
                >{"Wiki Ninja"}</p>
                <img
                    src={runningNinja}
                    alt={"running ninja"}
                    style={{
                        width: 210 * U,
                        position: "absolute",
                        left: leftTopPosition[0] + (100 * U),
                        top: leftTopPosition[1] + (470 * U),
                    }}
                    className="ninja"
                />
                <img
                    src={shuriken}
                    alt={"shuriken"}
                    style={{
                        width: 170 * U,
                        position: "absolute",
                        left: leftTopPosition[0] + (750 * U),
                        top: leftTopPosition[1] + (270 * U),
                    }}
                    className="shuriken"
                />
            </div >
        );
    }
}