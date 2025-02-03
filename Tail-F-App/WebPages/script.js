const socket = new WebSocket('http://localhost:5000/ws');
const messageContainer = document.getElementById('container');

socket.onopen = () => {
    console.log("Connected to web socket...");
    //socket.send("Hello....");
}

socket.onmessage = (event) => {
    messageContainer.textContent += event.data + "\n"
}

socket.onclose = () => {
    console.log("Connection closed.")
}