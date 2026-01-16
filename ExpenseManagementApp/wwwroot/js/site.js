// Chat functionality for Expense Management App
class ChatInterface {
    constructor() {
        this.messages = [];
        this.userId = 1; // Default user ID
        this.init();
    }

    init() {
        const form = document.getElementById('chat-form');
        const input = document.getElementById('chat-input');
        const messagesContainer = document.getElementById('chat-messages');

        if (!form || !input || !messagesContainer) return;

        form.addEventListener('submit', async (e) => {
            e.preventDefault();
            const message = input.value.trim();
            if (!message) return;

            this.addMessage('user', message);
            input.value = '';

            await this.sendMessage(message);
        });

        this.addMessage('assistant', 'Hello! I\'m your expense management assistant. I can help you view, create, submit, and manage expenses. What would you like to do?');
    }

    addMessage(role, content, functionResult = null) {
        const messagesContainer = document.getElementById('chat-messages');
        const messageDiv = document.createElement('div');
        messageDiv.className = `chat-message ${role === 'user' ? 'chat-message-user' : ''}`;

        const avatar = document.createElement('div');
        avatar.className = 'chat-message-avatar';
        avatar.textContent = role === 'user' ? 'U' : 'AI';

        const contentDiv = document.createElement('div');
        contentDiv.className = 'chat-message-content';
        
        if (functionResult) {
            contentDiv.innerHTML = `<div>${content}</div><pre style="margin-top: 0.5rem; padding: 0.5rem; background: rgba(0,0,0,0.05); border-radius: 0.25rem; overflow-x: auto;">${JSON.stringify(functionResult, null, 2)}</pre>`;
        } else {
            contentDiv.textContent = content;
        }

        messageDiv.appendChild(avatar);
        messageDiv.appendChild(contentDiv);
        messagesContainer.appendChild(messageDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;

        this.messages.push({ role, content });
    }

    async sendMessage(content) {
        const loadingDiv = document.createElement('div');
        loadingDiv.className = 'spinner';
        loadingDiv.id = 'loading-spinner';
        document.getElementById('chat-messages').appendChild(loadingDiv);

        try {
            const response = await fetch('/api/chat', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    messages: this.messages,
                    userId: this.userId
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            
            const spinner = document.getElementById('loading-spinner');
            if (spinner) spinner.remove();

            if (data.functionResult) {
                this.addMessage('assistant', data.message, data.functionResult);
            } else {
                this.addMessage('assistant', data.message);
            }
        } catch (error) {
            const spinner = document.getElementById('loading-spinner');
            if (spinner) spinner.remove();
            
            this.addMessage('assistant', `Error: ${error.message}. The AI service may not be configured.`);
        }
    }
}

// Initialize chat interface when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('chat-form')) {
        new ChatInterface();
    }
});

// Filter functionality for expense list
function applyFilters() {
    const statusId = document.getElementById('filter-status')?.value || '';
    const categoryId = document.getElementById('filter-category')?.value || '';
    const startDate = document.getElementById('filter-start-date')?.value || '';
    const endDate = document.getElementById('filter-end-date')?.value || '';

    const params = new URLSearchParams();
    if (statusId) params.append('statusId', statusId);
    if (categoryId) params.append('categoryId', categoryId);
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    window.location.href = `/ListExpenses?${params.toString()}`;
}

function clearFilters() {
    window.location.href = '/ListExpenses';
}

// Expense form validation
function validateExpenseForm() {
    const amount = document.getElementById('Amount')?.value;
    const category = document.getElementById('CategoryId')?.value;
    const date = document.getElementById('ExpenseDate')?.value;

    if (!amount || parseFloat(amount) <= 0) {
        alert('Please enter a valid amount');
        return false;
    }

    if (!category) {
        alert('Please select a category');
        return false;
    }

    if (!date) {
        alert('Please select an expense date');
        return false;
    }

    return true;
}

// Expense actions
async function submitExpense(expenseId) {
    if (!confirm('Are you sure you want to submit this expense for approval?')) {
        return;
    }

    try {
        const response = await fetch(`/api/expenses/${expenseId}/submit`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        });

        if (response.ok) {
            alert('Expense submitted successfully!');
            window.location.reload();
        } else {
            const error = await response.json();
            alert(`Error: ${error.detail || 'Failed to submit expense'}`);
        }
    } catch (error) {
        alert(`Error: ${error.message}`);
    }
}

async function approveExpense(expenseId, reviewerId) {
    if (!confirm('Are you sure you want to approve this expense?')) {
        return;
    }

    try {
        const response = await fetch(`/api/expenses/${expenseId}/approve`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ reviewerId: reviewerId || 2 })
        });

        if (response.ok) {
            alert('Expense approved successfully!');
            window.location.reload();
        } else {
            const error = await response.json();
            alert(`Error: ${error.detail || 'Failed to approve expense'}`);
        }
    } catch (error) {
        alert(`Error: ${error.message}`);
    }
}

async function rejectExpense(expenseId, reviewerId) {
    if (!confirm('Are you sure you want to reject this expense?')) {
        return;
    }

    try {
        const response = await fetch(`/api/expenses/${expenseId}/reject`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ reviewerId: reviewerId || 2 })
        });

        if (response.ok) {
            alert('Expense rejected successfully!');
            window.location.reload();
        } else {
            const error = await response.json();
            alert(`Error: ${error.detail || 'Failed to reject expense'}`);
        }
    } catch (error) {
        alert(`Error: ${error.message}`);
    }
}

async function deleteExpense(expenseId) {
    if (!confirm('Are you sure you want to delete this expense? This action cannot be undone.')) {
        return;
    }

    try {
        const response = await fetch(`/api/expenses/${expenseId}`, {
            method: 'DELETE'
        });

        if (response.ok || response.status === 204) {
            alert('Expense deleted successfully!');
            window.location.href = '/ListExpenses';
        } else {
            const error = await response.json();
            alert(`Error: ${error.detail || 'Failed to delete expense'}`);
        }
    } catch (error) {
        alert(`Error: ${error.message}`);
    }
}
